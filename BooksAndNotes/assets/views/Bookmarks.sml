<!-- StardewUI lays out the form and ribbon choices; no per-control screen coordinates. -->
<frame layout="stretch[..760] stretch[..680]"
       padding="24"
       background={@Mods/StardewUI/Sprites/ControlBorder}>
    <lane layout="stretch" orientation="vertical">
        <lane layout="stretch content" margin="0,0,0,16" vertical-content-alignment="middle">
            <label layout="stretch content" font="dialogue" text={#tab.bookmarks} />
            <button text={#button.close} left-click=|Close()| />
        </lane>
        <scrollable layout="stretch" peeking="64">
            <lane layout="stretch content" orientation="vertical">
                <lane *if={:CanEdit} layout="stretch content" orientation="vertical" margin="0,0,0,24">
                    <label text={:PageCaption} />
                    <label margin="0,8,0,4" text={#tab.name-label} />
                    <textinput layout="stretch 48px" text={<>Name} max-length={:MaxNameLength} placeholder="" />
                    <label margin="0,12,0,4" text={#tab.color} />
                    <grid layout="stretch content" item-layout="length: 88" item-spacing="8,8">
                        <frame *repeat={:Colors} padding="4" focusable="true" tooltip={:Name}
                               background={@Mods/StardewUI/Sprites/White} background-tint="Transparent"
                               +state:selected={Selected} +state:selected:background-tint="#FFD866"
                               left-click=|Choose()|>
                            <image layout="72px 42px" sprite={@Mods/Trapyy.BooksAndNotes/Sprites/Bookmarks:Ribbon} tint={:Tint} />
                        </frame>
                    </grid>
                    <lane margin="0,12,0,0">
                        <button text={:SaveText} left-click=|Save()|
                                pointer-events-enabled={CanSave}
                                opacity="0.5" +state:enabled={CanSave} +state:enabled:opacity="1" />
                        <button *if={:CanRemove} margin="12,0,0,0" text={#tab.remove} left-click=|Remove()| />
                    </lane>
                    <label *if={:LimitReached} layout="stretch content" margin="0,8,0,0" text={#tab.limit} />
                </lane>
            </lane>
        </scrollable>
    </lane>
</frame>
