using ShippingControl_v8.Models; 
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Maui;
using ShippingControl_v8.Commands; 
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace ShippingControl_v8.ViewModels
{
    public class OptimizationViewModel : ViewModelBase
    {
        private double _heightRequestPn = 50;
        public double HeightRequestPn 
        {
            get { return _heightRequestPn; }
            set 
            { 
                _heightRequestPn = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<AllocationPriorityItem> AllocationPriorityItems { get; set; } = new();
        public ICommand ChangeBoxesOrdination { get; }
        public ICommand ClickedPartNumber { get; }
        private bool _isOrdenationAscendent = false;
        private bool _showEveryPn = true;
        public bool ShowEveryPn
        {
            get => _showEveryPn;
            set
            {
                _showEveryPn = value;
                OnPropertyChanged();
            }
        }

        private bool _showOnePn = false;
        public bool ShowOnePn
        {
            get => _showOnePn;
            set
            {
                _showOnePn = value;
                OnPropertyChanged();
            }
        }

        public OptimizationViewModel() 
        {
            ShowEveryPn = true;
            ShowOnePn = false;

            AppSettings ApplicationSettings = new();
            ApplicationSettings.GetAppSettings();
            getMIB3(ApplicationSettings);

            ClickedPartNumber = new RelayCommand((parameter) =>
            {
                if (parameter is AllocationPriorityItem item)
                {
                    ShowEveryPn = false;
                    ShowOnePn = true;
                    getMIB3PartNumber(ApplicationSettings,item.PartNumber);
                }
            });

            ChangeBoxesOrdination = new RelayCommand(() =>
            {
                ShowEveryPn = true;
                ShowOnePn = false;
                string order = "asc";
                if (_isOrdenationAscendent) { order = "desc";}

                getMIB3(ApplicationSettings);
            });
        } 
        private void getMIB3(AppSettings ApplicationSettings)
        {
            AllocationPriorityItems.Clear();

            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
           .Append("PROGRAM_USER").Append(";data source=").Append(ApplicationSettings.szIPSelected)
           .Append(";Password=praga;persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
        
            try
            { 

                using (SqlConnection connection = cnn)
                {
                    using (SqlCommand cmd = new SqlCommand("pyms.[PROGRAM_USER].[GetIncompleteBinsNumberPNs]", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add any parameters if the stored procedure expects any
                        // Example:
                        // cmd.Parameters.Add(new SqlParameter("@ParamName", paramValue));

                        connection.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                AllocationPriorityItems.Add(new AllocationPriorityItem()
                                {
                                    PartNumber = reader["partnr"] as string,
                                    NumBins = (int)reader["NumberOfDifferentBins"],
                                });
                            }
                        }
                    }
                }

            }

            catch (Exception e)
            {
                var page = App.Current.MainPage;
                page.DisplayAlert("Erro", e.Message, "Ok");
            }
            finally
            { 

                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }

            }
        }
        private void getMIB3PartNumber(AppSettings ApplicationSettings, string partNumber)
        {
            AllocationPriorityItems.Clear(); 

            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand($"SELECT TOP(100)\r\n    [partnr],\r\n  wh.[BoxType],\r\n     [partnr_desc],\r\n    [PI],\r\n    wh.Bin,\r\n\tpki.BoxesNumber,\r\n    COUNT(*) AS TotalCombinations\r\nFROM [pyms].[dbo].[tbSAPPI] sp\r\n JOIN v_wh_bin_pn wh ON wh.PN = sp.partnr\r\n JOIN [PCL].[dbo].[PackingInstructionNr] pki ON pki.PackingInstruction = sp.PI COLLATE Latin1_General_CI_AS\r\nWHERE partnr_desc LIKE '%-M3%' AND partnr = '{partNumber}' \r\nGROUP BY [partnr], [partnr_desc], [PI], wh.Bin, wh.[BoxType], pki.BoxesNumber\r\nHAVING COUNT(*) < pki.BoxesNumber\r\norder by TotalCombinations DESC\r\n", cnn);
            SqlDataReader reader = null;

            try
            {

                cnn.Open();
                reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    AllocationPriorityItems.Add(new AllocationPriorityItem()
                    {
                        PartNumber = reader["partnr"] as string,
                        Bin = reader["Bin"] as string,
                        CountBoxes = (int)reader["TotalCombinations"],
                        MaxBoxes = (int)reader["BoxesNumber"],
                        BoxType = reader["BoxType"] as string,
                    });
                }
            }

            catch (Exception e)
            {
                var page = App.Current.MainPage;
                page.DisplayAlert("Erro", e.Message, "Ok");
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                {
                    reader.Close();
                }

                if (cmd != null)
                {
                    cmd.Dispose();
                }

                if (cnn != null && cnn.State == ConnectionState.Open)
                {
                    cnn.Close();
                    cnn.Dispose();
                }

            }
        }

    }
}
