using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System.Reflection;

namespace BooksAndNotes.Integrations;

/// <summary>A turn's source/destination page indices, direction, and native animation frame.</summary>
internal readonly record struct ParchmentTurn(int OldFirst, int NewFirst, bool Forward, int SpriteFrame);

/// <summary>The visible spread and opening-art frame used while the book closes.</summary>
internal readonly record struct ParchmentClose(int First, int OpenFrame);

/// <summary>Guards optional runtime drawing hooks against unsupported Parchment internals or artwork.</summary>
internal static class ParchmentTurnPatches
{
    private static Func<object, SpriteBatch, ParchmentTurn, bool> _drawTurn = null!;
    private static Func<object, SpriteBatch, ParchmentClose, bool> _drawClosing = null!;
    private static Func<object, bool> _ownsMenu = null!;
    private static IMonitor _monitor = null!;

    private static PropertyInfo _currentStateProperty = null!;
    private static object _turningState = null!;
    private static object _closingState = null!;
    private static object _coveringState = null!;
    private static FieldInfo _currentSpreadField = null!;
    private static FieldInfo _pendingSpreadField = null!;
    private static FieldInfo _currentChapterField = null!;
    private static FieldInfo _pendingChapterField = null!;
    private static FieldInfo _turningForwardField = null!;
    private static FieldInfo _animationFrameField = null!;
    private static FieldInfo _appearanceField = null!;
    private static PropertyInfo _frameWidthProperty = null!;
    private static PropertyInfo _frameHeightProperty = null!;
    private static PropertyInfo _openFrameCountProperty = null!;
    private static PropertyInfo _turnFrameCountProperty = null!;
    private static PropertyInfo _texturePathProperty = null!;

    private static bool _turnHookFailed;
    private static bool _closingHookFailed;
    private static bool _warnedAboutArtwork;

    /// <summary>Checks the framework's expected members, then installs turn and closing hooks independently.</summary>
    public static void Install(Harmony harmony, IMonitor monitor, Func<object, bool> owns,
        Func<object, SpriteBatch, ParchmentTurn, bool> draw, Func<object, SpriteBatch, ParchmentClose, bool> drawClosing)
    {
        _monitor = monitor;
        _drawTurn = draw;
        _drawClosing = drawClosing;
        _ownsMenu = owns;
        try
        {
            Type menuType = AccessTools.TypeByName("Parchment.Framework.UI.Menus.BookMenu")
                ?? throw new MissingMemberException("Parchment's BookMenu was not found.");
            MethodInfo drawPages = AccessTools.DeclaredMethod(menuType, "DrawPages", [typeof(SpriteBatch)])
                ?? throw new MissingMethodException(menuType.FullName, "DrawPages(SpriteBatch)");

            _currentStateProperty = menuType.GetProperty("CurrentState")
                ?? throw new MissingMemberException(menuType.FullName, "CurrentState");
            _turningState = Enum.Parse(_currentStateProperty.PropertyType, "Turning");
            _closingState = Enum.Parse(_currentStateProperty.PropertyType, "Closing");
            _coveringState = Enum.Parse(_currentStateProperty.PropertyType, "Covering");
            _currentSpreadField = GetRequiredField(menuType, "_currentSpread", typeof(int));
            _pendingSpreadField = GetRequiredField(menuType, "_pendingSpread", typeof(int));
            _currentChapterField = GetRequiredField(menuType, "_currentChapterIndex", typeof(int));
            _pendingChapterField = GetRequiredField(menuType, "_pendingChapterIndex", typeof(int));
            _turningForwardField = GetRequiredField(menuType, "_isTurningForward", typeof(bool));
            _animationFrameField = GetRequiredField(menuType, "_animationFrame", typeof(int));
            _appearanceField = AccessTools.DeclaredField(menuType, "_appearance")
                ?? throw new MissingFieldException(menuType.FullName, "_appearance");

            Type appearanceType = _appearanceField.FieldType;
            _frameWidthProperty = GetRequiredProperty(appearanceType, "FrameWidth", typeof(int));
            _frameHeightProperty = GetRequiredProperty(appearanceType, "FrameHeight", typeof(int));
            _openFrameCountProperty = GetRequiredProperty(appearanceType, "OpenFrameCount", typeof(int));
            _turnFrameCountProperty = GetRequiredProperty(appearanceType, "TurnFrameCount", typeof(int));
            _texturePathProperty = GetRequiredProperty(appearanceType, "TexturePath", typeof(string));
            harmony.Patch(drawPages, prefix: new HarmonyMethod(typeof(ParchmentTurnPatches), nameof(BeforeDrawPages)));

            try
            {
                MethodInfo drawBook = AccessTools.DeclaredMethod(menuType, "draw", [typeof(SpriteBatch)])
                    ?? throw new MissingMethodException(menuType.FullName, "draw(SpriteBatch)");
                // Closing skips DrawPages, so its ink needs a separate hook after the book sprite.
                harmony.Patch(drawBook, postfix: new HarmonyMethod(typeof(ParchmentTurnPatches), nameof(AfterDrawBook)));
            }
            catch (Exception exception)
            {
                _closingHookFailed = true;
                monitor.Log($"Closing-book ink is unavailable; page-turn ink remains enabled.\n{exception}", LogLevel.Warn);
            }
        }
        catch (Exception exception)
        {
            _turnHookFailed = true;
            monitor.Log($"Animated writing is unavailable with this Parchment build. Normal reading and editing remain available.\n{exception}", LogLevel.Warn);
        }
    }

    /// <summary>Finds an expected private field and rejects incompatible framework changes.</summary>
    private static FieldInfo GetRequiredField(Type type, string name, Type expectedType)
    {
        FieldInfo field = AccessTools.DeclaredField(type, name) ?? throw new MissingFieldException(type.FullName, name);
        if (field.FieldType != expectedType)
            throw new InvalidOperationException($"Parchment's {name} changed type.");

        return field;
    }

    /// <summary>Finds an expected appearance property and verifies its value type.</summary>
    private static PropertyInfo GetRequiredProperty(Type type, string name, Type expectedType)
    {
        PropertyInfo property = type.GetProperty(name) ?? throw new MissingMemberException(type.FullName, name);
        if (property.PropertyType != expectedType)
            throw new InvalidOperationException($"Parchment's {name} changed type.");

        return property;
    }

    /// <summary>Replaces native turn-page drawing only when this mod can render the supported turn safely.</summary>
    private static bool BeforeDrawPages(object __instance, SpriteBatch b)
    {
        // Harmony injects these parameters by name; keep __instance and the native argument name b.
        if (_turnHookFailed || !_ownsMenu(__instance))
            return true;

        try
        {
            if (!Equals(_currentStateProperty.GetValue(__instance), _turningState))
                return true;

            // Our generated readers have no chapters; chapter-relative indices are not document pages.
            if ((int)_currentChapterField.GetValue(__instance)! != 0 || (int)_pendingChapterField.GetValue(__instance)! != 0)
                return true;

            var turn = new ParchmentTurn((int)_currentSpreadField.GetValue(__instance)! * 2,
                (int)_pendingSpreadField.GetValue(__instance)! * 2,
                (bool)_turningForwardField.GetValue(__instance)!, (int)_animationFrameField.GetValue(__instance)!);
            if (!HasSupportedArtwork(__instance))
                return true;

            return !_drawTurn(__instance, b, turn);
        }
        catch (Exception exception)
        {
            _turnHookFailed = true;
            _monitor.Log($"Animated writing was disabled after a rendering error. Your saved writing is unchanged.\n{exception}", LogLevel.Warn);
            return true;
        }
    }

    /// <summary>Accepts only the calibrated small-book geometry and logs an unsupported skin once.</summary>
    private static bool HasSupportedArtwork(object menu)
    {
        object appearance = _appearanceField.GetValue(menu)!;
        bool supported = (int)_frameWidthProperty.GetValue(appearance)! == 219
            && (int)_frameHeightProperty.GetValue(appearance)! == 158
            && (int)_openFrameCountProperty.GetValue(appearance)! == 4
            && (int)_turnFrameCountProperty.GetValue(appearance)! == 6
            && (string.Equals((string?)_texturePathProperty.GetValue(appearance), ParchmentBookFactory.BookTexture, StringComparison.OrdinalIgnoreCase)
                || string.Equals((string?)_texturePathProperty.GetValue(appearance), "Assets/PeacefulEnd.Parchment/smallBook", StringComparison.OrdinalIgnoreCase));
        if (!supported && !_warnedAboutArtwork)
        {
            _monitor.Log("Animated writing requires Parchment's standard small-book artwork. Leaving this different layout to Parchment.", LogLevel.Warn);
            _warnedAboutArtwork = true;
        }

        return supported;
    }

    /// <summary>Adds ink to the native closing sprite and disables only closing ink if that drawing fails.</summary>
    private static void AfterDrawBook(object __instance, SpriteBatch b)
    {
        if (_turnHookFailed || _closingHookFailed || !_ownsMenu(__instance))
            return;

        try
        {
            object? state = _currentStateProperty.GetValue(__instance);
            if ((!Equals(state, _closingState) && !Equals(state, _coveringState)) || !HasSupportedArtwork(__instance))
                return;

            if ((int)_currentChapterField.GetValue(__instance)! != 0)
                return;

            _drawClosing(__instance, b, new ParchmentClose((int)_currentSpreadField.GetValue(__instance)! * 2,
                3 - (int)_animationFrameField.GetValue(__instance)!));
        }
        catch (Exception exception)
        {
            _closingHookFailed = true;
            _monitor.Log($"Closing-book ink was disabled after a rendering error. Page-turn ink and saved writing are unchanged.\n{exception}", LogLevel.Warn);
        }
    }
}
