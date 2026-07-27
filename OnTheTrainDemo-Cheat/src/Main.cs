using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(OnTheTrainDemoCheat.Main), "On The Train Demo Cheat", "1.5.12", "DestinyWind")]
[assembly: MelonGame("EastUpInteractive", "On The Train Demo")]

namespace OnTheTrainDemoCheat
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

            // 国际化：先把内嵌语言文件释放到磁盘（方便用户编辑），再按 Settings.Language 加载。
            I18n.ExtractEmbeddedFiles();
            I18n.Load(Settings.Language.Value);

            Patches.Install();   // static Harmony patches - registered once, no per-frame work
            MelonLogger.Msg("On The Train Demo Mod v1.5.12 loaded (fix: NetworkSceneObjectSpawner.Instance is a field, not a property).");
            MelonLogger.Msg("Press F5 to toggle the item browser, F6 to toggle the trainer menu. 当前语言：" + I18n.CurrentLanguage);
        }

        public override void OnUpdate()
        {
            // No per-frame cheat polling: God/Stamina/Ammo/Fuel are Harmony hooks now.
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
