
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using ShippingControl_v8.Commands;
using ShippingControl_v8.Models;
using ShippingControl_v8.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input; 

namespace ShippingControl_v8.ViewModels
{
    public class DocAllocationViewModel : ViewModelBase
    {
        private AppSettings ApplicationSettings = new();
        private string _tbBin = string.Empty;
        public string TbBin
        {
            get { return _tbBin; }
            set
            {
                _tbBin = value;
                OnPropertyChanged();
            }
        }

        private string _boxNumber = string.Empty;
        public string BoxNumber
        {
            get => _boxNumber;
            set
            {
                _boxNumber = value; 
                OnPropertyChanged();
            }
        }
        private string _tbDocNum = string.Empty;
        public string TbDocNum
        {
            get { return _tbDocNum; }
            set
            {
                _tbDocNum = value;
                OnPropertyChanged();
            }
        }
        private ObservableCollection<Box> _boxes = new();
        public ICommand DealocateDoc { get; }
        public DocAllocationViewModel() 
        {
            ApplicationSettings.GetAppSettings();

            MessagingCenter.Subscribe<object, string>(this, "ScannedEventTriggered", (sender, message) =>
            {
                ScanTriggered(message);
            });

            DealocateDoc = new RelayCommand(() =>
            {
                var page = App.Current.MainPage;
                try
                {
                    if (!TbDocNum.IsNullOrEmpty() && !TbBin.IsNullOrEmpty())
                    {
                        if (!CheckIsDocumentAllocated(TbDocNum)) { page.DisplayAlert("Erro", "Documento alocado previamente!", "Ok"); return; }

                        _boxes = GetBoxesOfDocument(TbDocNum);

                        if (IsMixedDoc(_boxes))
                        {
                            page.DisplayAlert("Erro", "Não pode alocar paletes mistas!", "Ok");
                            return;
                        }
                        if (_boxes.Count() > 0)
                        {
                            InsertBoxesFromDocument(_boxes);
                            SetDocAsAllocated(TbDocNum);
                        }
                        else
                        {
                            page.DisplayAlert("Erro", "Dados extraidos do documento inválidos!", "Ok");
                        }
                    }
                    else
                    {
                        page.DisplayAlert("Erro", "Informações em falta!", "Ok");
                    }
                }
                catch (WHException ex)
                {
                    page = App.Current.MainPage;
                    page.DisplayAlert("Erro", ex.Message, "Ok");
                }
                catch (Exception ex)
                {
                    page = App.Current.MainPage;
                    page.DisplayAlert("Erro", ex.Message, "Ok");
                }

            });
        }
        public void Unsubscribe()
        {
            MessagingCenter.Unsubscribe<object, string>(this, "ScannedEventTriggered");
        }
        public bool IsMixedDoc(ObservableCollection<Box> boxes) 
        {
            string prevPn = "InitialPartnumber";
            foreach(Box box in boxes)
            {
                if(prevPn != box.PartNumber && prevPn != "InitialPartnumber")
                {
                    return true;
                }
                else
                {
                    prevPn = box.PartNumber;
                }
            }
            return false;
        }
        public void ScanTriggered(string reading)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            { 
                try
                {
                    string box = "";
                    if (string.IsNullOrEmpty(reading)) return;
                      
                    switch (reading.Substring(0, 1))
                    {
                        // Pallet or Box with pn
                        case "A":
                            string[] codes = reading.Split(';');
                            if (codes.Length != 3) throw new Exception("Código 'A' com formato errado");
                            box = codes[2];
                            var doc = GetDocumentNrFromBox(box);
                            TbDocNum = doc;
                            var boxes = GetBoxesOfDocument(doc);
                            BoxNumber = boxes.Count() + " caixa(s)";
                            break;
                        case "M":
                        case "S":
                            box = reading.Substring(1, reading.Length - 1);
                            doc = GetDocumentNrFromBox(box);
                            TbDocNum = doc;
                            boxes = GetBoxesOfDocument(doc);
                             
                            BoxNumber = boxes.Count() + " caixa(s)";
                            break;
                        default:
                            // Bin
                            if (Regex.IsMatch(reading, @"\d{3}-\d{3}"))
                            {
                                TbBin = reading;
                            }
                            break;
                    } 
                }
                catch (Exception ex)
                {
                    var page = App.Current.MainPage;
                    await page.DisplayAlert("Erro", ex.Message, "Ok");
                }
            });
        }
        private string GetDocumentNrFromBox(string box)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand($"SELECT transp_doc\r\nFROM tbTranspDoc TD\r\nINNER JOIN tbPB B WITH (NOLOCK) ON TD.td_id = B.td_id\r\nWHERE pack_box = '{box}'", cnn);
            string docNum = "";
            SqlDataReader reader = null;

            try
            {
                cnn.Open();
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    docNum = reader["transp_doc"].ToString();
                }

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

                return docNum;
            }
            catch (SqlException e)
            {
                var page = App.Current.MainPage;
                page.DisplayAlert("Erro", e.Message, "Ok");
            } 
            catch(Exception e)
            {
                var page = App.Current.MainPage;
                page.DisplayAlert("Erro", e.Message, "Ok");
            } 

            return docNum;
             
        }
        private bool CheckIsDocumentAllocated(string docNumber)
        {
            bool allocated = false;
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=PROGRAM_USER;password=praga")
            .Append(";data source=").Append(ApplicationSettings.szIPSelected)
            .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand($"SELECT * FROM [pyms].[PROGRAM_USER].[BlockedTransDocNumbers]  WHERE doc_num = '{docNumber}'\r\n", cnn);

            SqlDataReader reader = null;
            try
            {
                cnn.Open();
                reader = cmd.ExecuteReader();
                allocated = false;
                while (reader.Read())
                {
                    allocated = true;
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
            return allocated;
        }
        private ObservableCollection<Box> GetBoxesOfDocument(string docNumber)
        {
            ObservableCollection<Box> boxes = new();

            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
            .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
            .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand($"SELECT TOP 100 pack_box, part_nr\r\nFROM tbTranspDoc TD WITH (NOLOCK)\r\nINNER JOIN tbPB B WITH (NOLOCK) ON TD.td_id = B.td_id\r\nWHERE transp_doc = '{docNumber}'\r\n", cnn);
      
            SqlDataReader reader = null;

            try
            {
                cnn.Open();
                reader = cmd.ExecuteReader();


                while (reader.Read())
                {
                    var box = reader["pack_box"].ToString();
                    var pn = reader["part_nr"].ToString();

                    if(box != null && pn != null) 
                    {
                        boxes.Add(new Box()
                        {
                            Name = box,
                            PartNumber = pn
                        });
                    }
                    
                }
                return boxes;
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
            return boxes;
        }
        private async void InsertBoxesFromDocument(ObservableCollection<Box> boxes)
        {
            var page = App.Current.MainPage;
            // New palete type information forms
            string palletTypeX = await page.DisplayActionSheet("TIPO DE PALETE?", "Cancel", null, "Azul", "Cinza", "Madeira", "Solta");
            string boxTypeX = await page.DisplayActionSheet("TIPO DE CAIXA?", "Cancel", null, "Original", "Cartão");

            PalletBoxType palletType = new PalletBoxType(palletTypeX, boxTypeX);

            foreach (Box box in boxes)
            {
                allocBoxOnBin(box.PartNumber, box.Name, "bin", false, true, palletType);
            } 
        }

        private void SetDocAsAllocated(string docNumber)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
            .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
            .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand($"INSERT INTO [pyms].[PROGRAM_USER].[BlockedTransDocNumbers] ([doc_num]) values ('{docNumber}')\r\n", cnn);

            SqlDataReader reader = null;
            try
            {
                cnn.Open(); 
                cmd.ExecuteNonQuery();
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

        private async void allocBoxOnBin(string partnumber, string box, string bin, bool closeBin, bool oneBox, PalletBoxType palletType)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("p_wh_alloc_pn_box_r1", cnn);

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@in_pn", SqlDbType.VarChar, 20).Value = partnumber;
                cmd.Parameters.Add("@in_box", SqlDbType.VarChar, 20).Value = box;
                cmd.Parameters.Add("@in_bin", SqlDbType.VarChar, 20).Value = bin;
                cmd.Parameters.Add("@in_closebin", SqlDbType.Bit).Value = false;
                cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 20).Value = ApplicationSettings.readUser;
                cmd.Parameters.Add("@in_one_box", SqlDbType.Bit).Value = oneBox;
                cmd.Parameters.Add("@in_boxtype", SqlDbType.VarChar, 20).Value = palletType.BoxType;
                cmd.Parameters.Add("@in_pallettype", SqlDbType.VarChar, 20).Value = palletType.PalletType;
                cmd.Parameters.Add("@out_result", SqlDbType.Int).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@out_error", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;

                cnn.Open();

                cmd.ExecuteNonQuery();

                if (((int)cmd.Parameters["@out_result"].Value) != 1)
                {
                    var page = App.Current.MainPage;
                    await page.DisplayAlert("Erro:", cmd.Parameters["@out_error"].Value.ToString(), "Ok");
                    throw new WHException(cmd.Parameters["@out_error"].Value as string);
                }
            }
            catch (SqlException ex)
            {
                throw;
            }
            finally
            {
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
