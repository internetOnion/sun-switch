#if UNITY_EDITOR
namespace SunSwitch.UI.Preview
{
    // Verification fixture: intentionally leaves requests pending to test duplicate protection.
    public sealed class MenuFlowProbe : MenuFlowAdapter
    {
        public int requests;
        public override void RequestStart(StartMenuView view) { requests++; }
        public override void RequestQuit(StartMenuView view) { requests++; }
    }
}
#endif
