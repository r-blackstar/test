using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(OnTheTrainDemoMod.Main), "On The Train Demo Mod", "1.5.6", "DestinyWind")]
[assembly: MelonGame("EastUpInteractive", "On The Train Demo")]

namespace OnTheTrainDemoMod
{
    public class Main : MelonMod
    {
        private bool _menuOpen;
        private bool _browserOpen;
        private readonly KeyCode _menuKey = KeyCode.F6;
        private readonly KeyCode _browserKey = KeyCode.F5;

        public override void OnInitializeMelon()
        {
            Settings.Register();

            I18n.ExtractEmbeddedFiles();
            I18n.Load(Settings.Language.Value);

            Patches.Install();
            MelonLogger.Msg("On The Train Demo Mod v1.5.6 loaded (godmode-fix + vitals + freecraft + full item i18n + categorized display).");
            MelonLogger.Msg("Press F5 to toggle the item browser, F6 to toggle the trainer menu. 当前语言：" + I18n.CurrentLanguage);
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(_menuKey))
                _menuOpen = !_menuOpen;

            if (Input.GetKeyDown(_browserKey))
                _browserOpen = !_browserOpen;
        }

        public override void OnGUI()
        {
            MenuUI.Draw(ref _menuOpen);
            ItemBrowserUI.Draw(ref _browserOpen);
        }
    }
}
