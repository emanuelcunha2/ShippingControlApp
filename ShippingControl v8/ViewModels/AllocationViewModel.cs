
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
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShippingControl_v8.ViewModels
{
    public class AllocationViewModel : ViewModelBase
    {
        public ObservableCollection<Fifo> FifoList { get; set; } = new ObservableCollection<Fifo>();

        private AppSettings ApplicationSettings = new();
        private string _tbPartNumber = string.Empty;
        public string tbPartNumber
        {
            get { return _tbPartNumber; }
            set
            {
                _tbPartNumber = value;
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
        private string _tbBin1 = string.Empty;
        public string tbBin1
        {
            get { return _tbBin1; }
            set
            {
                _tbBin1 = value;
                OnPropertyChanged();
            }
        }
        private string _tbBin2 = string.Empty;
        public string tbBin2
        {
            get { return _tbBin2; }
            set
            {
                _tbBin2 = value;
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
            qtyPThrough = 0;
        }

        private void partialReset()
        {
            boxes.Clear();
            tbPartNumber = "";
            tbBox = "";
            btBx = "0 PEÇAS";
            isPassThrough = false;
            qtyPThrough = 0;
        }
        public ICommand OpenBoxesList { get; }
        public ICommand chkCloseBin1Checked { get; }
        public ICommand chkCloseBin2Checked { get; }
        public ICommand SaveButtonPressed { get; }
        public ICommand ResetButtonPressed { get; }
        public ICommand VerifyBin { get; }
        public AllocationViewModel(INavigation pageNavigation)
        {
            ApplicationSettings.GetAppSettings();
     
            chkCloseBin1Checked = new RelayCommand(() =>
            {
                chkCloseBin1 = !chkCloseBin1;
            });

            chkCloseBin2Checked = new RelayCommand(() =>
            {
                chkCloseBin2 = !chkCloseBin2;
            });

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

            ResetButtonPressed = new RelayCommand(() =>
            {
                reset();
            });

            VerifyBin = new RelayCommand(async () =>
            {
                if (tbPartNumber.IsNullOrEmpty()) { return; }

                getPnFIFO(tbPartNumber);

                string resString = "";

                int countBoxes = 0;
                string type = "";
                string isFull = "";

                foreach (Fifo fifo in FifoList)
                {
                    countBoxes = fifo.boxes.Where(box => box.PartNumber == tbPartNumber).Count();
                    type = fifo.boxes.Where(box => box.PartNumber == tbPartNumber).FirstOrDefault()?.type;
                    isFull = fifo.IsFull == "False" ? "LIVRE" : "CHEIO";

                    var countBoxesString = countBoxes < 10 ? "0" + countBoxes.ToString() : countBoxes.ToString();
                    resString += fifo.Bin + " | " + countBoxesString + " CAIXA(S) " + type.ToUpper() + "\n" + isFull + "\n\n";
                }

                await App.Current.MainPage.DisplayAlert("BINs FIFO", resString, "Ok");
            });

            SaveButtonPressed = new RelayCommand(async () =>
            {
                await allocOnBins();
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
                            if (!string.IsNullOrEmpty(error = verifyIfAllocated(tbPartNumber, tbBox)))
                            {
                                // Display Error
                                var page = App.Current.MainPage;
                                await page.DisplayAlert("Erro", error, "Ok");
                                // Reset All Data
                                reset();
                                return;
                            }

                            // Find if its reading Pallets or boxes
                            isPallet = tbBox.Length == 9 && tbBox.StartsWith("5");

                            // If it a pallet resets boxes
                            if (isPallet) boxes.Clear();

                            qty = getBoxQty(tbPartNumber, tbBox, isPallet);
                            boxes.Add(new BoxQty(tbPartNumber, tbBox, qty));

                            // Suggest a bin for that partnumber
                            lbResult = hintBin(tbPartNumber);
                            break;
                        case "M":
                        // Pallet or Box without pn
                        case "S":
                            if (!string.IsNullOrEmpty(tbPartNumber))
                            {
                                this.tbBox = reading.Substring(1, reading.Length - 1);

                                // Check if its already alocated
                                if (boxAlreadyRead(tbPartNumber, tbBox)) throw new Exception("Caixa já foi lida");
                                if (!string.IsNullOrEmpty(error = verifyIfAllocated(tbPartNumber, tbBox)))
                                {
                                    // Display Error
                                    var page = App.Current.MainPage;
                                    await page.DisplayAlert("Erro", error, "Ok");
                                    // Reset All Data
                                    reset();
                                    return;
                                }

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
                            // Check if it matches this format of PN
                            if (Regex.IsMatch(reading, @"^(0000)?(28|29)\d{6}(-\d{2})?$"))
                            {
                                partialReset();
                                processPassThrough(reading);
                            }
                            break;
                        default:
                            // Bin
                            if (Regex.IsMatch(reading, @"\d{3}-\d{3}"))
                            {
                                this.tbBin1 = reading;
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
                                    // Check if its already alocated
                                    if (!string.IsNullOrEmpty(error = verifyIfAllocated(tbPartNumber, tbBox)))
                                    {
                                        // Display Error
                                        var page = App.Current.MainPage;
                                        await page.DisplayAlert("Erro", error, "Ok");
                                        // Reset All Data
                                        reset();
                                        return;
                                    }
                                    qty = getBoxQty(tbPartNumber, tbBox, isPallet);
                                    boxes.Add(new BoxQty(tbPartNumber, tbBox, qty));
                                }
                                // Suggest a bin for that partnumber
                                lbResult = hintBin(tbPartNumber);
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
        private async Task allocOnBins()
        {
            try
            {
                string allocResult = "";
                var page = App.Current.MainPage;
                // Check information filled
                if (string.IsNullOrEmpty(this.tbPartNumber)) throw new Exception("Falta o número de peça.");
                if (string.IsNullOrEmpty(this.tbBin1)) throw new Exception("Falta o número de bin.");

                // New palete type information forms
                string palletTypeX = await page.DisplayActionSheet("TIPO DE PALETE?", "Cancel", null, "Azul", "Cinza", "Madeira", "Solta");
                string boxTypeX = await page.DisplayActionSheet("TIPO DE CAIXA?", "Cancel", null, "Original", "Cartão");

                PalletBoxType palletType = new PalletBoxType(palletTypeX, boxTypeX);

                if (isPassThrough)
                {
                    if (qtyPThrough <= 0) throw new Exception("Falta quantidade para alocação.");

                    allocPassthrough(this.tbPartNumber, this.tbBin1, qtyPThrough);

                    if (!string.IsNullOrEmpty(this.tbBin2)
                        && this.tbBin1 != this.tbBin2)
                    {
                        allocPassthrough(this.tbPartNumber, this.tbBin2, qtyPThrough);
                    }
                    StringBuilder result = new StringBuilder();
                    result.Append(tbPartNumber).Append("/").Append(qtyPThrough + " modulo(s)");
                    result.Append(" alocado(s) no(s) bin(s) ");
                    result.Append(tbBin1);
                    if (!string.IsNullOrEmpty(tbBin2)) result.Append(", ").Append(tbBin2);

                    // Write the response
                    lbResult = result.ToString();
                }
                else
                {
                    if (string.IsNullOrEmpty(this.tbBox)) throw new Exception("Falta o número de palete/caixa.");

                    bool isPallet = tbBox.Length == 9 && tbBox.StartsWith("5");

                    if (isPallet)
                    {
                        regPalIn(this.tbPartNumber, this.tbBox, ApplicationSettings.readUser);

                        allocPalletOnBin(this.tbPartNumber, this.tbBox, this.tbBin1, this.chkCloseBin1, palletType);

                        if (!string.IsNullOrEmpty(this.tbBin2)
                            && this.tbBin1 != this.tbBin2)
                        {
                            allocPalletOnBin(this.tbPartNumber, this.tbBox, this.tbBin2, this.chkCloseBin2, palletType);
                        }
                        StringBuilder result = new StringBuilder();
                        result.Append(tbPartNumber).Append("/").Append(tbBox);
                        result.Append(" alocado no(s) bin(s) ");
                        result.Append(tbBin1);
                        if (!string.IsNullOrEmpty(tbBin2)) result.Append(", ").Append(tbBin2);

                        // Write the response
                        lbResult = result.ToString();
                    }
                    else
                    {
                        if (boxes.Count > 1)
                        {
                            Dictionary<string, int> pns = partnumbersCount(boxes);
                            if (pns.Count > 1)
                            {
                                bool answerResult = await page.DisplayAlert("Confirmar", "Atenção.Mais do que um número de peça lido. Quer continuar?", "Sim", "Não");
                                if (!answerResult)
                                {
                                    reset();
                                    return;
                                }
                            }

                            foreach (var item in boxes)
                            {
                                regBoxIn(item.PN, item.Box, ApplicationSettings.readUser);

                                allocResult = allocBoxOnBin(item.PN, item.Box, this.tbBin1, this.chkCloseBin1, true, palletType);

                                if (!string.IsNullOrEmpty(this.tbBin2)
                                    && this.tbBin1 != this.tbBin2)
                                {
                                    allocResult = allocBoxOnBin(this.tbPartNumber, item.Box, this.tbBin2, this.chkCloseBin2, true, palletType);
                                }
                            }
                            StringBuilder result = new StringBuilder();
                            if (allocResult == "")
                            {
                                bool first = true;
                                foreach (var item in pns)
                                {
                                    if (first) first = false;
                                    else result.Append(", ");

                                    result.Append(item.Key).Append("/").Append(boxes.Count);
                                }
                                result.Append(boxes.Count).Append(" caixas");
                                result.Append(" alocadas no(s) bin(s) ");
                                result.Append(tbBin1);
                                if (!string.IsNullOrEmpty(tbBin2)) result.Append(", ").Append(tbBin2);
                                // Write the response
                                lbResult = result.ToString();
                            }
                            else
                            {
                                // Write the response
                                lbResult = allocResult.ToString();
                            }
                        }
                        else
                        {
                            regBoxIn(this.tbPartNumber, this.tbBox, ApplicationSettings.readUser);

                            bool oneBox = true;

                            allocResult = allocBoxOnBin(this.tbPartNumber, this.tbBox, this.tbBin1, this.chkCloseBin1, oneBox, palletType);

                            if (!string.IsNullOrEmpty(this.tbBin2)
                                && this.tbBin1 != this.tbBin2
                                && !oneBox)
                            {
                                allocResult = allocBoxOnBin(this.tbPartNumber, this.tbBox, this.tbBin2, this.chkCloseBin2, oneBox, palletType);
                            }

                            if (allocResult == "")
                            {
                                StringBuilder result = new StringBuilder();
                                result.Append(tbPartNumber).Append("/").Append(tbBox);
                                result.Append(" alocado no(s) bin(s) ");
                                result.Append(tbBin1);
                                if (!string.IsNullOrEmpty(tbBin2)) result.Append(", ").Append(tbBin2);
                                // Write the response
                                lbResult = result.ToString();
                            }
                            else
                            {
                                // Write the response
                                lbResult = allocResult.ToString();
                            }
                        }
                    }
                }
            }
            catch (WHException ex)
            {
                var page = App.Current.MainPage;
                await page.DisplayAlert("Erro", ex.Message, "Ok");
            }
            catch (Exception ex)
            {
                var page = App.Current.MainPage;
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
                cmd.Parameters.Add("@in_closebin", SqlDbType.Bit).Value = false;
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

                BoxQuantityPage form = new BoxQuantityPage(true, reading);
                form.ModalClosed += (s, args) =>
                {
                    if (((BoxQuantityViewModel)form.BindingContext).WasModalConfirmed)
                    {
                        tbPartNumber = reading;
                        qtyPThrough = ((BoxQuantityViewModel)form.BindingContext).Qty;
                        isPassThrough = true;

                        // Suggest a bin for that partnumber
                        lbResult = hintBin(tbPartNumber);
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



        private void getPnFIFO(string text)
        {
            FifoList.Clear();
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("SELECT Bin, PN, Box, IsFull, BoxType, FIFO FROM v_wh_bin_pn WHERE PN like '%' + @pn + '%' ORDER BY  Bin, fifo  ", cnn);
            SqlDataReader reader = null;

            try
            {
                cmd.Parameters.Add("@pn", SqlDbType.VarChar).Value = text;

                cnn.Open();
                reader = cmd.ExecuteReader();

                string pn = null;
                string bin = null;
                string prevPn = null;
                string prevBin = null;
                DateTime prevDate = DateTime.MinValue;

                int order = 0;
                DateTime fifoDate;

                Fifo currentFifo = new();

                while (reader.Read())
                {
                    pn = reader["PN"] as string;
                    bin = reader["Bin"] as string;
                    var stringTest = reader["FIFO"].ToString();

                    if (!stringTest.IsNullOrEmpty())
                    {
                        fifoDate = (DateTime)reader["FIFO"];
                    }
                    else { fifoDate = DateTime.MinValue; }

                    if (bin != prevBin)
                    {
                        // Add previous fifo
                        if (prevPn != null && bin != null)
                        {
                            FifoList.Add(currentFifo);
                        }

                        if (fifoDate > prevDate)
                        {
                            prevDate = fifoDate;
                        }
                        order++;

                        currentFifo = new Fifo()
                        {
                            Order = order.ToString(),
                            Date = fifoDate.ToString("dd/MM/yyyy HH:mm:ss"),
                            Bin = bin,
                            IsFull = reader["IsFull"].ToString()
                        };

                        prevPn = pn;
                        prevBin = bin;
                    }

                    string box = reader["Box"] as string;
                    currentFifo.boxes.Add(new Box()
                    {
                        Name = box,
                        Date = fifoDate.ToString("dd/MM/yyyy HH:mm:ss"),
                        FifoDate = fifoDate,
                        Bin = bin,
                        PartNumber = pn,
                        type = reader["BoxType"].ToString().IsNullOrEmpty() ? "ERR" : reader["BoxType"].ToString()
                    });

                }
                FifoList.Add(currentFifo);

                // Order FIFO
                var orderedFifoList = FifoList.OrderBy(bin => bin.boxes.Min(box => box.FifoDate)).ToList();
                FifoList.Clear();

                int countOrder = 1;
                foreach (Fifo orderedFifo in orderedFifoList)
                {
                    orderedFifo.Order = countOrder.ToString();
                    FifoList.Add(orderedFifo);

                    countOrder++;
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
            catch (Exception)
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


        private string hintBin(string partnumber)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("SELECT dbo.fn_hint_bin (@pn) AS Bin", cnn);
            SqlDataReader reader = null;

            try
            {
                int padding = 12 - partnumber.Length;
                if (padding > 0)
                {
                    partnumber = new string('0', padding) + partnumber;
                }

                cmd.Parameters.Add("@pn", SqlDbType.VarChar, 20).Value = partnumber;
                cnn.Open();
                reader = cmd.ExecuteReader();

                StringBuilder hint = new StringBuilder("Sugestão Bin: ");
                bool first = true;

                int i = 0;

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
         
      
        private string allocBoxOnBin(string partnumber, string box, string bin, bool closeBin, bool oneBox, PalletBoxType palletType)
        {
            string result = "";
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

                var outError = cmd.Parameters["@out_error"].Value;
                if (outError != null)
                {
                    var page = App.Current.MainPage;
                    if(!cmd.Parameters["@out_error"].Value.ToString().IsNullOrEmpty())
                    {
                        result = cmd.Parameters["@out_error"].Value.ToString();
                    }
                    else
                    {
                        result = "Erro de Base de dados, verifique se a caixa já se encontra alocada e se o bin existe";
                    }
                   
                    page.DisplayAlert("Erro", result, "Ok");
                    return result;
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
            return result;
        }


        
    }
}
