
using Microsoft.Data.SqlClient;
using ShippingControl_v8.Commands;
using ShippingControl_v8.Models;
using ShippingControl_v8.Views; 
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions; 
using System.Windows.Input;


namespace ShippingControl_v8.ViewModels
{
    public class DesallocationViewModel : ViewModelBase
    {
        private AppSettings ApplicationSettings = new();
        private string _tbPartNumber = string.Empty;
        public string tbPartNumber
        {
            get { return _tbPartNumber; }
            set
            {
                _tbPartNumber = value.Replace("\n", "");
                OnPropertyChanged();
            }
        }
        private string _tbBox = string.Empty;
        public string tbBox
        {
            get { return _tbBox; }
            set
            {
                _tbBox = value.Replace("\n", "");
                OnPropertyChanged();
            }
        }

        private string _binPThrough;
        public string binPThrough
        {
            get { return _binPThrough; }
            set
            {
                _binPThrough = value.Replace("\n", "");
                OnPropertyChanged();
            }
        }

        private string _tbBin1 = string.Empty;
        public string tbBin1
        {
            get { return _tbBin1; }
            set
            {
                _tbBin1 = value.Replace("\n", "");
                OnPropertyChanged();
            }
        }
        private string _tbBin2 = string.Empty;
        public string tbBin2
        {
            get { return _tbBin2; }
            set
            {
                _tbBin2 = value.Replace("\n", "");
                OnPropertyChanged();
            }
        }
        private bool _chkCloseBin1 = false;
        public bool chkCloseBin1
        {
            get { return _chkCloseBin1; }
            set
            {
                _chkCloseBin1 = value;
                OnPropertyChanged();
            }
        }
        private bool _chkCloseBin2 = false;
        public bool chkCloseBin2
        {
            get { return _chkCloseBin2; }
            set
            {
                _chkCloseBin2 = value;
                OnPropertyChanged();
            }
        }
        private bool _isPassThrough = false;
        public bool isPassThrough
        {
            get { return _isPassThrough; }
            set
            {
                _isPassThrough = value;
                OnPropertyChanged();
            }
        }
        private int _qtyPThrough = 0;
        public int qtyPThrough
        {
            get { return _qtyPThrough; }
            set
            {
                _qtyPThrough = value;
                OnPropertyChanged();
            }
        }
        private string _btBx = "0 PEÇAS";
        public string btBx
        {
            get { return _btBx; }
            set
            {
                _btBx = value;
                OnPropertyChanged();
            }
        }
        private string _lbResult = "LEITURA DISPONIVEL";
        public string lbResult
        {
            get { return _lbResult; }
            set
            {
                _lbResult = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<BoxQty> boxes = new ObservableCollection<BoxQty>();

        public void Unsubscribe()
        {
            MessagingCenter.Unsubscribe<object, string>(this, "ScannedEventTriggered");
        }
        private void reset()
        {
            boxes.Clear();
            tbPartNumber = "";
            tbBox = "";
            tbBin1 = "";
            tbBin2 = "";
            chkCloseBin1 = false;
            chkCloseBin2 = false;
            btBx = "0 PEÇAS";
            isPassThrough = false;
            binPThrough = "";
            qtyPThrough = 0;
        }

        private void partialReset()
        {
            boxes.Clear();
            tbPartNumber = "";
            tbBox = "";
            btBx = "0 PEÇAS";
            isPassThrough = false;
            binPThrough = "";
            qtyPThrough = 0;
        }
        public ICommand OpenBoxesList { get; } 
        public ICommand SaveButtonPressed { get; }
        public ICommand ResetButtonPressed { get; }
        public ICommand OpenFifo { get; }
        public DesallocationViewModel(INavigation pageNavigation)
        {
            ApplicationSettings.GetAppSettings();

            OpenBoxesList = new RelayCommand(async () =>
            {
                BoxesPage form = new(boxes);
                form.ModalClosed += (s, args) =>
                {
                    if (boxes.Count == 0) { tbBox = ""; }
                    updateBoxQty();
                };
                await pageNavigation.PushModalAsync(form);

            });

            OpenFifo = new RelayCommand(async() =>
            {
                FifoPage form = new FifoPage(tbPartNumber);
                await pageNavigation.PushAsync(form);
            });

            ResetButtonPressed = new RelayCommand(() =>
            {
                reset();
            });

            SaveButtonPressed = new RelayCommand( async() =>
            {
                await deallocOnBins();
                reset();
            });

            MessagingCenter.Subscribe<object, string>(this, "ScannedEventTriggered", (sender, message) =>
            {
                ScanTriggered(message);
            });
        }
        public void ScanTriggered(string reading)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (isProcessingThePassThrough) return;

                try
                {
                    if (string.IsNullOrEmpty(reading)) return;

                    bool isPallet = false;
                    int qty = 0;

                    switch (reading.Substring(0, 1))
                    {
                        // Pallet or Box with pn
                        case "A":
                            string[] codes = reading.Split(';');
                            if (codes.Length != 3) throw new Exception("Código 'A' com formato errado");

                            string pn = codes[0].Substring(1, codes[0].Length - 1);
                            this.tbPartNumber = pn;

                            this.tbBox = codes[2];

                            // Check if box was already read
                            if (boxAlreadyRead(tbPartNumber, tbBox)) throw new Exception("Caixa já foi lida");

                            // Check if its already alocated
                            string error = null;

                            // Find if its reading Pallets or boxes
                            isPallet = tbBox.Length == 9 && tbBox.StartsWith("5");

                            // If it a pallet resets boxes
                            if (isPallet) boxes.Clear();

                            qty = getBoxQty(tbPartNumber, tbBox, isPallet);
                            boxes.Add(new BoxQty(tbPartNumber, tbBox, qty));

                            // bin for that partnumber
                            lbResult = showWhereAllocated(tbPartNumber);
                            break;
                        case "M":
                        // Pallet or Box without pn
                        case "S":
                            if (!string.IsNullOrEmpty(tbPartNumber))
                            {
                                this.tbBox = reading.Substring(1, reading.Length - 1);

                                // Check if its already alocated
                                if (boxAlreadyRead(tbPartNumber, tbBox)) throw new Exception("Caixa já foi lida");

                                // Find if its reading Pallets or boxes
                                isPallet = tbBox.Length == 9 && tbBox.StartsWith("5");

                                // If it a pallet resets boxes
                                if (isPallet) boxes.Clear();

                                qty = getBoxQty(tbPartNumber, tbBox, isPallet);
                                boxes.Add(new BoxQty(tbPartNumber, tbBox, qty));
                            }
                            break;
                        // Its a Part Number
                        case "P":
                            reading = reading.Remove(0, 1);
                            if (Regex.IsMatch(reading, @"^(0000)?(28|29)\d{6}(-\d{2})?$"))
                            {
                                reset();
                                processPassThrough(reading);
                            }
                            break;
                        default:
                            if (Regex.IsMatch(reading, @"^(((0000)?\d{8})|(\d{8}\-\d{2}))$"))
                            {
                                this.tbPartNumber = reading;
                                lbResult = showWhereAllocated(tbPartNumber);
                            }
                            // Partnumber started with 30S
                            else if (reading.StartsWith("30S"))
                            {
                                this.tbPartNumber = reading.Substring(3, reading.Length - 3);
                                // If box/palett nr is filled
                                if (!string.IsNullOrEmpty(tbBox))
                                {
                                    // Check if its already read
                                    if (boxAlreadyRead(tbPartNumber, tbBox)) throw new Exception("Caixa já foi lida");

                                    qty = getBoxQty(tbPartNumber, tbBox, isPallet);
                                    boxes.Add(new BoxQty(tbPartNumber, tbBox, qty));
                                }
                                // Suggest a bin for that partnumber
                                lbResult = showWhereAllocated(tbPartNumber);
                            }
                            break;
                    }

                    updateBoxQty();
                }
                catch (Exception ex)
                {
                    var page = App.Current.MainPage;
                    await page.DisplayAlert("Erro", ex.Message, "Ok"); 
                }
            });
    
        }

        // Alloc on bins on save
        private async Task deallocOnBins()
        {
            var page = App.Current.MainPage;
            try
            {
                if (string.IsNullOrEmpty(this.tbPartNumber)) throw new Exception("Falta o número de peça.");
                if (string.IsNullOrEmpty(this.tbBox)) throw new Exception("Falta o número de palete/caixa.");

                if (isPassThrough)
                {
                    bool outsideFIFO = false;
                    
                    if (string.IsNullOrEmpty(binPThrough)) throw new Exception("Falta o número de bin de origem.");
                    if (qtyPThrough <= 0) throw new Exception("Falta quantidade para alocação.");

                    if (!verifyFIFO(this.tbPartNumber, this.tbBox, false, false))
                    {
                        bool answerResult = await page.DisplayAlert("Confirmar", "Está a tentar desalocar uma palete sem respeitar o FIFO. Quer continuar?", "Sim", "Não");
                        if (!answerResult)
                        { 
                            return;
                        }
                        outsideFIFO = true;
                    }

                    deallocPassthrough(this.tbPartNumber, binPThrough, qtyPThrough);

                    if (outsideFIFO)
                    {
                        string notify = string.Format("User {0} deallocated box/pallet {1}/{2} outside FIFO.", ApplicationSettings.readUser, this.tbBox, this.tbPartNumber);
                        Task.Run(() => { sendEmail(notify); });
                        saveNoFIFONotification(this.tbBox, this.tbPartNumber, notify);
                    }

                    StringBuilder result = new StringBuilder();
                    result.Append(tbPartNumber).Append("/").Append(qtyPThrough + " modulo(s)");
                    result.Append(" desalocado com sucesso.");

                    // Write the response
                    lbResult = result.ToString();
                }
                else
                {

                    bool isPallet = tbBox.Length == 9 && tbBox.StartsWith("5");

                    bool outsideFIFO = false;

                    if (isPallet)
                    {
                        if (!verifyFIFO(this.tbPartNumber, this.tbBox, isPallet, false))
                        {
                            bool answerResult = await page.DisplayAlert("Confirmar", "Está a tentar desalocar uma palete sem respeitar o FIFO. Quer continuar?", "Sim", "Não");
                            if (!answerResult)
                            {
                                return;
                            }
                            outsideFIFO = true;
                        }

                        deallocPallet(this.tbPartNumber, this.tbBox);

                        if (outsideFIFO)
                        {
                            string notify = string.Format("User {0} deallocated box/pallet {1}/{2} outside FIFO.", ApplicationSettings.readUser, this.tbBox, this.tbPartNumber);
                            Task.Run(() => { sendEmail(notify); });
                            saveNoFIFONotification(this.tbBox, this.tbPartNumber, notify);
                        }

                        StringBuilder result = new StringBuilder();
                        result.Append(tbPartNumber).Append("/").Append(tbBox);
                        result.Append(" desalocado com sucesso.");

                        // Write the response
                        lbResult = result.ToString();
                    }
                    else
                    {
                        if (boxes.Count > 1)
                        {
                            List<BoxQty> boxesOutFIFO = new List<BoxQty>();
                            foreach (var item in boxes)
                            {
                                if (!verifyFIFO(item.PN, item.Box, isPallet, true))
                                {
                                    bool answerResult = await page.DisplayAlert("Confirmar", "Está a tentar desalocar esta caixa sem respeitar o FIFO. Quer continuar?", "Sim", "Não");
                                    if (!answerResult)
                                    {
                                        return;
                                    }
                                    outsideFIFO = true;
                                    boxesOutFIFO.Add(item);
                                }
                            }

                            foreach (var item in boxes)
                            {
                                deallocBox(item.PN, item.Box, true);

                                if (outsideFIFO && boxesOutFIFO.Contains(item))
                                {
                                    string notify = string.Format("User {0} deallocated box/pallet {1}/{2} outside FIFO.", ApplicationSettings.readUser, item.Box, item.PN);

                                    // Start a new task to perform some background work
                                    Task.Run(() => 
                                    { 
                                        sendEmail(notify);
                                        saveNoFIFONotification(item.Box, item.PN, notify);
                                    }); 
                                }
                            }

                            StringBuilder result = new StringBuilder();
                            result.Append(boxes.Count).Append(" caixas");
                            result.Append(" desalocadas com sucesso.");
                            // Write the response
                            lbResult = result.ToString();
                        }
                        else
                        {
                            bool oneBox = await page.DisplayAlert("Confirmar", "Quer desalocar apenas uma caixa?", "Sim", "Não");

                            if (!verifyFIFO(tbPartNumber, this.tbBox, isPallet, oneBox))
                            {
                                bool answerResult = await page.DisplayAlert("Confirmar", "Está a tentar desalocar uma palete sem respeitar o FIFO. Quer continuar?", "Sim", "Não");
                                if (!answerResult)
                                {
                                    return;
                                }

                                outsideFIFO = true;
                            }

                            deallocBox(this.tbPartNumber, this.tbBox, oneBox);

                            if (outsideFIFO)
                            {
                                string notify = string.Format("User {0} deallocated box/pallet {1}/{2} outside FIFO.", ApplicationSettings.readUser, this.tbBox, this.tbPartNumber);
                                Task.Run(() => { sendEmail(notify); });
                                saveNoFIFONotification(this.tbBox, this.tbPartNumber, notify);
                            }

                            StringBuilder result = new StringBuilder();
                            result.Append(tbPartNumber).Append("/").Append(tbBox);
                            result.Append(" desalocado com sucesso.");
                            // Write the response
                            lbResult = result.ToString();
                        }
                    }
                }

                string fifo = showWhereAllocated(tbPartNumber);
                if (!string.IsNullOrEmpty(fifo)) 
                {
                    // Write the response
                    lbResult = fifo.ToString();
                }
                 
            }
            catch (WHException ex)
            {
                await page.DisplayAlert("Erro", ex.Message, "Ok");
            }
            catch (Exception ex)
            {
                await page.DisplayAlert("Erro", ex.Message, "Ok");
            }
        }


        private bool regBoxIn(string sApar, string sCaixa, string user)
        {
            try
            {
                SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                    .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                    .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());

                SqlCommand cmd = new SqlCommand("p_pack_arrival_pb2", cnn);

                try
                {
                    cnn.Open();

                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@in_packbox", SqlDbType.VarChar, 20);
                    cmd.Parameters.Add("@in_partnr", SqlDbType.VarChar, 20);
                    cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 10);
                    cmd.Parameters.Add("@out_td", SqlDbType.VarChar, 14);
                    cmd.Parameters.Add("@out_result", SqlDbType.Int);

                    cmd.Parameters["@in_packbox"].Value = sCaixa;
                    cmd.Parameters["@in_partnr"].Value = sApar;
                    cmd.Parameters["@in_user"].Value = user;
                    cmd.Parameters["@out_td"].Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters["@out_result"].Direction = System.Data.ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    int result = -1;
                    result = Convert.ToInt32(cmd.Parameters["@out_result"].Value);

                    return result < 0;

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
            catch (System.Exception exp)
            {
                var page = App.Current.MainPage;
                page.DisplayAlert("Erro", exp.Message, "Ok");

                return false;
            }
        }


        private Dictionary<string, int> partnumbersCount(ObservableCollection<BoxQty> boxes)
        {
            Dictionary<string, int> pns = new Dictionary<string, int>();
            foreach (var item in boxes)
            {
                if (pns.ContainsKey(item.PN))
                {
                    pns[item.PN]++;
                }
                else
                {
                    pns.Add(item.PN, 1);
                }
            }
            return pns;
        }


        private void allocPalletOnBin(string partnumber, string pallet, string bin, bool closeBin, PalletBoxType palletType)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("p_wh_alloc_pn_pallet_r1", cnn);

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@in_pn", SqlDbType.VarChar, 20).Value = partnumber;
                cmd.Parameters.Add("@in_pallet", SqlDbType.VarChar, 20).Value = pallet;
                cmd.Parameters.Add("@in_bin", SqlDbType.VarChar, 20).Value = bin;
                cmd.Parameters.Add("@in_closebin", SqlDbType.Bit).Value = closeBin;
                cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 20).Value = ApplicationSettings.readUser;
                cmd.Parameters.Add("@in_boxtype", SqlDbType.VarChar, 20).Value = palletType.BoxType;
                cmd.Parameters.Add("@in_pallettype", SqlDbType.VarChar, 20).Value = palletType.PalletType;
                cmd.Parameters.Add("@out_result", SqlDbType.Int).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@out_error", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;

                cnn.Open();

                cmd.ExecuteNonQuery();

                if (((int)cmd.Parameters["@out_result"].Value) != 1)
                {
                    throw new WHException(cmd.Parameters["@out_error"].Value as string);
                }

            }
            catch (Exception)
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

        private void allocPassthrough(string partnumber, string bin, int qty)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("p_wh_alloc_pn_pthrough", cnn);

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@in_pn", SqlDbType.VarChar, 20).Value = partnumber;
                cmd.Parameters.Add("@in_bin", SqlDbType.VarChar, 20).Value = bin;
                cmd.Parameters.Add("@in_qty", SqlDbType.SmallInt).Value = qty;
                cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 20).Value = ApplicationSettings.readUser;
                cmd.Parameters.Add("@out_result", SqlDbType.Int).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@out_error", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;

                cnn.Open();

                cmd.ExecuteNonQuery();

                if (((int)cmd.Parameters["@out_result"].Value) != 1)
                {
                    throw new WHException(cmd.Parameters["@out_error"].Value as string);
                }

            }
            catch (Exception)
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
        private void updateBoxQty()
        {
            int totalQty = 0;
            foreach (var item in boxes)
            {
                totalQty += item.Qty;
            }
            this.btBx = string.Format("{0} PEÇAS", totalQty);
        }


        private bool isProcessingThePassThrough = false;
        private void processPassThrough(string reading)
        {
            try
            {
                isProcessingThePassThrough = true;

                BoxQuantityPage form = new BoxQuantityPage(false, reading);
                form.ModalClosed += (s, args) =>
                {
                    if (((BoxQuantityViewModel)form.BindingContext).WasModalConfirmed)
                    {
                        tbPartNumber = reading;
                        qtyPThrough = ((BoxQuantityViewModel)form.BindingContext).Qty;
                        binPThrough = ((BoxQuantityViewModel)form.BindingContext).Bin;

                        short pt_qty = 0;
                        tbBox = getBoxQtyPThrough(tbPartNumber, binPThrough, ref pt_qty);

                        if (string.IsNullOrEmpty(tbBox) || tbBox.Length < 9 || tbBox[4] != '9')
                            throw new Exception("Caixa/Palete não é passthrough ou não foi alocada.");

                        qtyPThrough = Math.Min(pt_qty, qtyPThrough);
                        boxes.Add(new BoxQty(tbPartNumber, tbBox, qtyPThrough));
                        isPassThrough = true;

                        // Suggest a bin for that partnumber
                        lbResult = showWhereAllocated(tbPartNumber);
                        isProcessingThePassThrough = false;
                    }
                    else
                    {
                        reset();
                    }
                };
            }
            catch
            {
                throw;
            }
        }

        private bool boxAlreadyRead(string pn, string box)
        {
            foreach (var item in boxes)
            {
                if (item.PN == pn && item.Box == box) return true;
            }
            return false;
        }

        private string verifyIfAllocated(string partnumber, string box)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("SELECT DISTINCT Bin FROM v_wh_bin_pn WHERE PN = @pn AND Box = @box", cnn);
            SqlDataReader reader = null;

            try
            {
                int padding = 12 - partnumber.Length;
                if (padding > 0)
                {
                    partnumber = new string('0', padding) + partnumber;
                }

                cmd.Parameters.Add("@pn", SqlDbType.VarChar, 20).Value = partnumber;
                cmd.Parameters.Add("@box", SqlDbType.VarChar, 20).Value = box;
                cnn.Open();
                reader = cmd.ExecuteReader();

                int i = 0;

                StringBuilder hint = new StringBuilder("Pallete/Caixa já alocada em: ");
                bool first = true;

                while (reader.Read())
                {
                    if (first) first = false;
                    else hint.Append(",");

                    hint.Append(reader["Bin"] as string);
                    i++;
                }
                if (i == 0) return null;
                else return hint.ToString();

            }
            catch
            {
                return null;
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

        private int getBoxQty(string partnumber, string box, bool isPallet)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            string query = (isPallet) ? "SELECT SUM(nr_modules) AS nr_modules FROM tbPB WHERE part_nr = @pn AND pallete_nr = @box"
                : "SELECT nr_modules FROM tbPB WHERE part_nr = @pn AND pack_box = @box";
            SqlCommand cmd = new SqlCommand(query, cnn);
            SqlDataReader reader = null;

            try
            {
                cnn.Open();

                cmd.Parameters.Add("@pn", SqlDbType.VarChar, 20).Value = (partnumber.Length == 8) ? "0000" + partnumber : partnumber;
                cmd.Parameters.Add("@box", SqlDbType.VarChar, 20).Value = box;

                reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return Convert.ToInt32(reader["nr_modules"]);
                }
                return 0;
            }
            catch (SqlException ex)
            {
                return 0;
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

                if (cnn != null)
                {
                    if (cnn.State == ConnectionState.Open) cnn.Close();
                    cnn.Dispose();
                }

            }
        }
        private bool regPalIn(string sApar, string sPal, string user)
        {
            try
            {
                SqlConnection conn = new SqlConnection("workstation id=\"Scan\";packet size=4096;user id="
                    + ApplicationSettings.sUser + ";data source=" + ApplicationSettings.szIPSelected
                    + ";persist security info=False;TrustServerCertificate=true;initial catalog=" + ApplicationSettings.sDB);
                try
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("p_pack_arrival_pallete2", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@in_pallete", SqlDbType.VarChar, 20);
                    cmd.Parameters.Add("@in_partnr", SqlDbType.VarChar, 20);
                    cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 10);
                    cmd.Parameters.Add("@out_td", SqlDbType.VarChar, 14);
                    cmd.Parameters.Add("@out_result", SqlDbType.Int);

                    cmd.Parameters["@in_pallete"].Value = sPal;
                    cmd.Parameters["@in_partnr"].Value = sApar;
                    cmd.Parameters["@in_user"].Value = user;
                    cmd.Parameters["@out_td"].Direction = System.Data.ParameterDirection.Output;
                    cmd.Parameters["@out_result"].Direction = System.Data.ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    int result = Convert.ToInt32(cmd.Parameters["@out_result"].Value);

                    return (result < 0);

                }
                finally
                {
                    if (conn != null)
                    {
                        conn.Close();
                    }
                }

            }
            catch (System.Exception exp)
            {
                var page = App.Current.MainPage;
                page.DisplayAlert("Erro", exp.Message, "Ok");

                return false;
            }
        }

        private string showWhereAllocated(string partnumber)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());

            StringBuilder query = new StringBuilder("SELECT Bin, MIN(FIFO) AS FIFO FROM v_wh_bin_pn ")
                .Append("WHERE PN = @pn AND FIFO < DATEADD(d,1,(SELECT MIN(FIFO) FROM v_wh_bin_pn WHERE PN = @pn)) ")
                .Append("GROUP BY Bin ORDER BY FIFO, Bin ASC");
            SqlCommand cmd = new SqlCommand(query.ToString(), cnn);
            SqlDataReader reader = null;

            try
            {
                if (partnumber.Length == 8) partnumber = "0000" + partnumber;

                cmd.Parameters.Add("@pn", SqlDbType.VarChar, 20).Value = partnumber;
                cnn.Open();
                reader = cmd.ExecuteReader();

                StringBuilder hint = new StringBuilder(partnumber).Append(" alocado em : ");
                bool first = true;

                int i = 0;

                while (reader.Read())
                {
                    if (first) first = false;
                    else hint.Append(" / ");

                    hint.Append(reader["Bin"] as string);
                    i++;
                }
                if (i == 0) return null;
                return hint.ToString();

            }
            catch
            {
                return null;
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

        private string getBoxQtyPThrough(string partnumber, string bin, ref short qty)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            StringBuilder query = new StringBuilder("SELECT tbPB.pack_box, tbPB.nr_modules, tbWHBins.WHBin ")
                .Append("FROM dbo.tbPB INNER JOIN dbo.tbWHBinsPB ON dbo.tbPB.pb_id = dbo.tbWHBinsPB.pb_id INNER JOIN dbo.tbWHBins ON dbo.tbWHBinsPB.WHbin_id = dbo.tbWHBins.WHBin_id ")
                .Append("WHERE tbPB.part_nr = @pn AND tbWHBins.WHBin = @bin");
            SqlCommand cmd = new SqlCommand(query.ToString(), cnn);
            SqlDataReader reader = null;

            qty = 0;

            try
            {
                cnn.Open();

                cmd.Parameters.Add("@pn", SqlDbType.VarChar, 20).Value = (partnumber.Length == 8) ? "0000" + partnumber : partnumber;
                cmd.Parameters.Add("@bin", SqlDbType.VarChar, 20).Value = bin;

                reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    qty = Convert.ToInt16(reader["nr_modules"]);
                    return reader["pack_box"] as string;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
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

                if (cnn != null)
                {
                    if (cnn.State == ConnectionState.Open) cnn.Close();
                    cnn.Dispose();
                }

            }
        }


        private void deallocPassthrough(string partnumber, string bin, int qty)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("p_wh_dealloc_pn_pthrough", cnn);

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@in_pn", SqlDbType.VarChar, 20).Value = partnumber;
                cmd.Parameters.Add("@in_qty", SqlDbType.SmallInt).Value = qty;
                cmd.Parameters.Add("@in_from_bin", SqlDbType.VarChar, 15).Value = bin;
                cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 20).Value = ApplicationSettings.readUser;
                cmd.Parameters.Add("@in_ver_fifo", SqlDbType.Bit).Value = false;
                cmd.Parameters.Add("@out_result", SqlDbType.Int).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@out_error", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;

                cnn.Open();

                cmd.ExecuteNonQuery();

                if (((int)cmd.Parameters["@out_result"].Value) != 1)
                {
                    throw new Exception(cmd.Parameters["@out_error"].Value as string);
                }

            }
            catch (Exception)
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


        private bool verifyFIFO(string pn, string box, bool isPallet, bool oneBox)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());

            string query = null;
            if (isPallet) query = "SELECT TOP 1 pn_fifo.FIFO, box_arrival.Arrival, CAST((SELECT Value FROM tbParam WHERE Parameter = 'WH_FIFO_days') AS int) AS days FROM v_wh_bin_pn AS pn_fifo INNER JOIN v_wh_bin_pn AS box_arrival ON pn_fifo.PN = box_arrival.PN WHERE pn_fifo.PN = @pn AND box_arrival.Pallet = @box ORDER BY pn_fifo.FIFO";
            else if (oneBox) query = @"SELECT TOP 1 pn_fifo.FIFO, box_arrival.Arrival, CAST((SELECT Value FROM tbParam WHERE Parameter = 'WH_FIFO_days') AS int) AS days FROM v_wh_bin_pn AS pn_fifo INNER JOIN v_wh_bin_pn AS box_arrival ON pn_fifo.PN = box_arrival.PN WHERE pn_fifo.PN = @pn AND box_arrival.Box = @box ORDER BY pn_fifo.FIFO";
            else query = "SELECT TOP 1 pn_fifo.FIFO, bin_arrival.Arrival, CAST((SELECT Value FROM tbParam WHERE Parameter = 'WH_FIFO_days') AS int) AS days FROM v_wh_bin_pn AS pn_fifo INNER JOIN v_wh_bin_pn AS box_arrival ON pn_fifo.PN = box_arrival.PN INNER JOIN v_wh_bin_pn AS bin_arrival ON box_arrival.Bin = bin_arrival.Bin WHERE pn_fifo.PN = @pn AND box_arrival.Box = @box ORDER BY pn_fifo.FIFO, bin_arrival.Arrival";
            SqlCommand cmd = new SqlCommand(query, cnn);

            SqlDataReader reader = null;

            try
            {
                if (pn.Length == 8) pn = "0000" + pn;
                cmd.Parameters.Add("@pn", SqlDbType.VarChar, 20).Value = pn;
                cmd.Parameters.Add("@box", SqlDbType.VarChar, 20).Value = box;
                cnn.Open();

                reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    DateTime fifo = (DateTime)reader["FIFO"];
                    DateTime arrival = (DateTime)reader["Arrival"];
                    int days = (int)reader["days"];

                    if (arrival <= fifo.AddDays(days)) return true;
                }

                return false;
            }
            catch (Exception)
            {
                return true;
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

        private void sendEmail(string msg)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("wharehouse.braga@delphi.com");
                foreach (var item in getEmailRecipients())
                {
                    mail.To.Add(new MailAddress(item));
                }
                mail.BodyEncoding = System.Text.Encoding.Default;
                mail.Body = msg;
                mail.Subject = "Wharehouse FIFO";

                SmtpClient client = new SmtpClient("10.238.226.250", 5000);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Credentials = new SmtpCredential("tester", "logo1", "GSE-ED1C10-1");

                client.Send(mail);
            }
            catch (Exception)
            {
            }
        }

        private IEnumerable<string> getEmailRecipients()
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("SELECT Value FROM tbParam WHERE [Parameter] = 'WH_Email'", cnn);
            SqlDataReader reader = null;

            List<string> recipients = new List<string>();

            try
            {

                cnn.Open();
                reader = cmd.ExecuteReader();


                if (reader.Read())
                {
                    string[] recStrings = reader["Value"].ToString().Split(';');
                    if (recStrings != null && recStrings.Length > 0)
                    {
                        recipients.AddRange(recStrings);
                    }
                }

                return recipients;
            }
            catch (Exception)
            {
                return recipients;
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

        private void saveNoFIFONotification(string box, string pn, string msg)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("INSERT INTO tbWHNotif (notif_user, bin, box, partnr, type, msg) VALUES (@user, @bin, @box, @partnr, @type, @msg)", cnn);

            try
            {
                cnn.Open();
                cmd.Parameters.Add("@user", SqlDbType.VarChar, 20).Value = ApplicationSettings.readUser;
                cmd.Parameters.Add("@bin", SqlDbType.VarChar, 20).Value = DBNull.Value;
                cmd.Parameters.Add("@box", SqlDbType.VarChar, 20).Value = box;
                cmd.Parameters.Add("@partnr", SqlDbType.VarChar, 20).Value = pn;
                cmd.Parameters.Add("@type", SqlDbType.TinyInt).Value = 0;
                cmd.Parameters.Add("@msg", SqlDbType.VarChar, 255).Value = msg;

                cmd.ExecuteNonQuery();
            }
            catch
            {
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



        private void deallocPallet(string partnumber, string box)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("p_wh_dealloc_pn_pallet", cnn);

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@in_pn", SqlDbType.VarChar, 20).Value = partnumber;
                cmd.Parameters.Add("@in_pallet", SqlDbType.VarChar, 20).Value = box;
                cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 20).Value = ApplicationSettings.readUser;
                cmd.Parameters.Add("@in_ver_fifo", SqlDbType.Bit).Value = false;
                cmd.Parameters.Add("@out_result", SqlDbType.Int).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@out_error", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;

                cnn.Open();

                cmd.ExecuteNonQuery();

                if (((int)cmd.Parameters["@out_result"].Value) != 1)
                {
                    throw new Exception(cmd.Parameters["@out_error"].Value as string);
                }

            }
            catch (Exception)
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


        private void deallocBox(string partnumber, string box, bool oneBox)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("p_wh_dealloc_pn_box", cnn);

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@in_pn", SqlDbType.VarChar, 20).Value = partnumber.Replace("\n", "");
                cmd.Parameters.Add("@in_box", SqlDbType.VarChar, 20).Value = box.Replace("\n","");
                cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 20).Value = ApplicationSettings.readUser;
                cmd.Parameters.Add("@in_one_box", SqlDbType.Bit).Value = oneBox;
                cmd.Parameters.Add("@in_ver_fifo", SqlDbType.Bit).Value = false;
                cmd.Parameters.Add("@out_result", SqlDbType.Int).Direction = ParameterDirection.Output;
                cmd.Parameters.Add("@out_error", SqlDbType.VarChar, 255).Direction = ParameterDirection.Output;

                cnn.Open();

                cmd.ExecuteNonQuery();

                if (((int)cmd.Parameters["@out_result"].Value) != 1)
                {
                    throw new Exception(cmd.Parameters["@out_error"].Value as string);
                }

            }
            catch (SqlException ex)
            {
                var page = App.Current.MainPage;
                page.DisplayAlert("Erro", ex.Message, "Ok");
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
