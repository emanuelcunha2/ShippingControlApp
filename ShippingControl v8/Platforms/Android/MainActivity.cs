using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using AndroidX.AppCompat.App;
using Symbol.XamarinEMDK;
using Symbol.XamarinEMDK.Barcode;
using System.Text;
using System.Xml;

namespace ShippingControl_v8;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity, EMDKManager.IEMDKListener, EMDKManager.IStatusListener
{
    private EMDKManager emdkManager;
    private ProfileManager profileManager = null;
    StringBuilder sb;
    private Symbol.XamarinEMDK.Notification.NotificationManager notificationManager;
    private BarcodeManager barcodeManager;
    private Scanner scanner;

    void EMDKManager.IEMDKListener.OnClosed()
    {
        if (emdkManager != null)
        {
            emdkManager.Release();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // Clean up the objects created by EMDK manager
        if (profileManager != null)
        {
            profileManager = null;
        }

        if (emdkManager != null)
        {
            emdkManager.Release();
            emdkManager = null;
        }

        if (this.scanner != null)
        {
            scanner.Data -= Scanner_Data;
            scanner.Status -= Scanner_Status;
             
        }
    }

    void EMDKManager.IEMDKListener.OnOpened(EMDKManager emdkManagerInstance)
    {
        this.emdkManager = emdkManagerInstance;
        this.barcodeManager = (BarcodeManager)emdkManagerInstance.GetInstance(EMDKManager.FEATURE_TYPE.Barcode);
        this.scanner = this.barcodeManager.GetDevice(BarcodeManager.DeviceIdentifier.Default);

        if (this.scanner != null)
        {
            // Configure the scanner
            this.scanner.Status += Scanner_Status;
            this.scanner.Data += Scanner_Data;
            this.scanner.Enable();
            this.scanner.Read();
        }

        try
        {
            emdkManager.GetInstanceAsync(EMDKManager.FEATURE_TYPE.Profile, this);
            emdkManager.GetInstanceAsync(EMDKManager.FEATURE_TYPE.Version, this);
            emdkManager.GetInstanceAsync(EMDKManager.FEATURE_TYPE.Notification, this);
        }
        catch (Exception e)
        { 
            Console.WriteLine("Exception: " + e.StackTrace);
        }
    }

    private void Scanner_Data(object sender, Scanner.DataEventArgs e)
    {
        if (e.P0 != null && e.P0.Result == ScannerResults.Success)
        {
            var scanData = e.P0.GetScanData();
            string text = "";

            foreach (ScanDataCollection.ScanData data in scanData)
            {
                text += data.Data.ToString();
            }

            // Send a message to MainPage to execute the function
            MessagingCenter.Send<object, string>(this, "ScannedEventTriggered", text);


            Console.WriteLine("" + text);
        }
    }

    private void Scanner_Status(object sender, Scanner.StatusEventArgs e)
    {
        var state = e.P0.State;
        if (state == StatusData.ScannerStates.Idle)
        {
            try
            {
                Thread.Sleep(100);
                this.scanner.Read();

            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }
    }


    void EMDKManager.IStatusListener.OnStatus(EMDKManager.StatusData statusData, EMDKBase emdkBase)
    {
        if (statusData.Result == EMDKResults.STATUS_CODE.Success)
        {
            if (statusData.FeatureType == EMDKManager.FEATURE_TYPE.Profile)
            {
                profileManager = (ProfileManager)emdkBase;
                profileManager.Data += ProfileManager_Data;
                string[] modifyData = new string[1];
                EMDKResults results = profileManager.ProcessProfileAsync("SOMESETTING", ProfileManager.PROFILE_FLAG.Set, modifyData);
                sb.AppendLine("ProcessProfileAsync:" + results.StatusCode);

            }

            if (statusData.FeatureType == EMDKManager.FEATURE_TYPE.Version)
            {
                versionManager = (VersionManager)emdkBase;
                String emdkVersion = versionManager.GetVersion(VersionManager.VERSION_TYPE.Emdk);
                String mxVersion = versionManager.GetVersion(VersionManager.VERSION_TYPE.Mx);
                sb.AppendLine("Versions: EMDK=" + emdkVersion + " MX=" + mxVersion);

            }

            if (statusData.FeatureType == EMDKManager.FEATURE_TYPE.Notification)
            {
                notificationManager = (Symbol.XamarinEMDK.Notification.NotificationManager)emdkBase;

                foreach (Symbol.XamarinEMDK.Notification.DeviceInfo di in notificationManager.SupportedDevicesInfo)
                    sb.AppendLine("Notifications info: NAME=" + di.FriendlyName + " TYPE=" + di.DeviceType);
            }

        }
    }

    void ProfileManager_Data(object sender, ProfileManager.DataEventArgs e)
    {
        EMDKResults results = e.P0.Result;
        sb.AppendLine("onData:" + CheckXmlError(results));
        long end_time = DateTime.Now.Ticks;
        sb.AppendLine("Time spent: " + (end_time - begin_time) / 10000 + "msec");


        //var toast = Toast.MakeText(this, sb.ToString(), ToastLength.Long);
        //toast.Show();

    }


    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        sb = new StringBuilder();
        Platforms.Android.Resources.DangerousTrustProvider.Register();
    }
    protected override void OnResume()
    {
        base.OnResume();

    }

    protected override void OnPause()
    {
        base.OnPause(); 

        PowerManager powerManager = (PowerManager)GetSystemService(Context.PowerService);
        KeyguardManager keyguardManager = (KeyguardManager)GetSystemService(Context.KeyguardService);

        bool isKeyguardLocked = keyguardManager.IsKeyguardLocked;

        bool isScreenOn = powerManager.IsInteractive;

        if (!isScreenOn || isKeyguardLocked)
        { 

        }
        else
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

    } 

    public VersionManager versionManager { get; private set; }
    long begin_time = 0;
    protected override void OnPostCreate(Bundle savedInstanceState)
    {
        base.OnPostCreate(savedInstanceState);
        begin_time = DateTime.Now.Ticks;

        //adb shell content query --uri content://settings/system/screen_off_timeout

        // The EMDKManager object will be created and returned in the callback
        EMDKResults results = EMDKManager.GetEMDKManager(this, this);
        sb.AppendLine("GetEMDKManager:" + results.StatusCode);
    }

    String QueryAndroidSystemSettings(Android.Net.Uri uri)
    { //e.g."content://settings/system/screen_off_timeout"
        string[] projection = new string[] { "name" };
        //Android.Net.Uri.Builder _ub = new Android.Net.Uri.Builder();
        //Android.Net.Uri uri = _ub.Path(key).Build();
        ICursor syssetCursor = ContentResolver.Query(uri, null, null, null, null);

        string text = "";
        if (syssetCursor.MoveToFirst())
        {
            text = syssetCursor.GetString(2);
        }
        return text;

    }

    private string CheckXmlError(EMDKResults results)
    {
        StringReader stringReader = null;
        string checkXmlStatus = "";
        bool isFailure = false;

        try
        {
            if (results.StatusCode == EMDKResults.STATUS_CODE.CheckXml)
            {
                stringReader = new StringReader(results.StatusString);

                using (XmlReader reader = XmlReader.Create(stringReader))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "parm-error":
                                    isFailure = true;
                                    string parmName = reader.GetAttribute("name");
                                    string parmErrorDescription = reader.GetAttribute("desc");
                                    checkXmlStatus = "Name: " + parmName + ", Error Description: " + parmErrorDescription;
                                    break;
                                case "characteristic-error":
                                    isFailure = true;
                                    string errorType = reader.GetAttribute("type");
                                    string charErrorDescription = reader.GetAttribute("desc");
                                    checkXmlStatus = "Type: " + errorType + ", Error Description: " + charErrorDescription;
                                    break;
                            }
                        }
                    }

                    if (!isFailure)
                    {
                        checkXmlStatus = "Profile applied successfully ...";
                    }

                }
            }
            else
            {
                checkXmlStatus = results.StatusCode.ToString();
            }
        }
        finally
        {
            if (stringReader != null)
            {
                stringReader.Dispose();
            }
        }

        return checkXmlStatus;
    }


}