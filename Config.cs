using GTA;
using System.Windows.Forms;
using System.Globalization;

namespace DualWield
{
  public class Config : Script
  {
    private static readonly ScriptSettings iniFile = ScriptSettings.Load("scripts\\DualWield.ini");
    public static readonly Keys toggleKey = iniFile.GetValue("Config", "ToggleKey", Keys.X, CultureInfo.InvariantCulture);
    public static readonly float recoil = iniFile.GetValue("Config", "FakeRecoil", 0f, CultureInfo.InvariantCulture);
    public static readonly bool pitchAnims = iniFile.GetValue("Config", "UpDownAimAnimation", true, CultureInfo.InvariantCulture);
  }
}
