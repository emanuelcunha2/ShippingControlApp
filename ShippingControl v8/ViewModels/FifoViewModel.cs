
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Maui;
using ShippingControl_v8.Commands;
using ShippingControl_v8.Models; 
using System.Collections.ObjectModel;
using System.Data; 
using System.Text;
using System.Text.RegularExpressions; 
using System.Windows.Input; 

namespace ShippingControl_v8.ViewModels
{
    class FifoViewModel : ViewModelBase
    {
        private string _partNumber = string.Empty;
        public string PartNumber
        {
            get { return _partNumber; }
            set 
            { 
                _partNumber = value;
                OnPropertyChanged();
            }
        }
        private bool _isCleanBoxVisible = false;
        public bool IsCleanBoxVisible
        {
            get { return _isCleanBoxVisible; }
            set
            {
                _isCleanBoxVisible = value;
                OnPropertyChanged();
            }
        }


        private bool _isCleanBinVisibile = false;
        public bool IsCleanBinVisible
        {
            get { return _isCleanBinVisibile; }
            set
            {
                _isCleanBinVisibile = value;
                OnPropertyChanged();
            }
        }

        private bool _chkAllBoxes = false;
        public bool chkAllBoxes
        {
            get => _chkAllBoxes;
            set
            {
                _chkAllBoxes = value;
                OnPropertyChanged();
            }
        }

        private AppSettings ApplicationSettings = new();
        public ICommand SearchPartNumber { get; }
        public ICommand CleanBoxClicked { get; }
        public ICommand CleanBinClicked { get; }
        public ICommand BoxSelected { get; }
        public ICommand BoxSelected2 { get; }
        public ICommand PartNumberSelected { get; }
        public ICommand BinSelected { get; }
        public ICommand AllBoxesPressed { get; }
        public ICommand CloseThisBin { get; }
        public ICommand CloseThisPartNumber { get; }

        private Box _selectedBox = new();
        public Box SelectedBox
        {
            get { return _selectedBox; }
            set
            {
                _selectedBox = value;
                OnPropertyChanged();
            }
        }

        private Fifo _selectedBin = new();
        public Fifo SelectedBin
        {
            get { return _selectedBin; }
            set
            {
                _selectedBin = value;
                OnPropertyChanged();
            }
        }

        private Fifo _selectedPartNumber = new();
        public Fifo SelectedPartNumber
        {
            get { return _selectedPartNumber; }
            set
            {
                _selectedPartNumber = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Fifo> FifoList { get; set; } = new ObservableCollection<Fifo>();
        private bool _isBinOpened = false;
        private bool _isPnOpened = false;

        private bool _justReadPartNumber = false;
        public bool JustReadPartNumber
        {
            get { return _justReadPartNumber;}
            set
            {
                _justReadPartNumber = value;

                IsCleanBoxVisible = true;
                IsCleanBinVisible = true;

                OnPropertyChanged();
            }
        }
        private bool _justReadBin = false;
        public bool JustReadBin
        {
            get { return _justReadBin;}
            set
            {
                _justReadBin = value;

                IsCleanBoxVisible = true;
                IsCleanBinVisible = true;

                OnPropertyChanged();
            }
        } 
        public void Unsubscribe()
        {
            MessagingCenter.Unsubscribe<object, string>(this, "ScannedEventTriggered");
        }

        public ICommand ShowHelpTip { get; }
        public FifoViewModel(string partNumber) 
        {
            MessagingCenter.Subscribe<object, string>(this, "ScannedEventTriggered", (sender, message) =>
            {
                ScanTriggered(message);
            });


            ApplicationSettings.GetAppSettings();

            
            if(!partNumber.IsNullOrEmpty())
            {
                PartNumber = partNumber;
                SearchText();
            }

            AllBoxesPressed = new RelayCommand(() => 
            {
                chkAllBoxes = !chkAllBoxes;
            });

            CloseThisPartNumber = new RelayCommand((parameter) =>
            {
                if (parameter is Fifo senderPN)
                {
                    if (!_isPnOpened) 
                    { 
                        OpenThisPn(senderPN);
                    }
                    else
                    {
                        if (senderPN == SelectedPartNumber)
                        {
                            SelectedPartNumber.DisplayedBoxes.Clear(); 
                            _isPnOpened = false;
                        }
                        else { OpenThisPn(senderPN); }
                    }
                }
            });

            CloseThisBin = new RelayCommand((parameter) =>
            {
                if (parameter is Fifo senderBin)
                {
                    if (!_isBinOpened) 
                    {
                        OpenThisBin(senderBin); 
                    }
                    else
                    {
                        if (senderBin == SelectedBin)
                        {
                            SelectedBin.DisplayedBoxes.Clear(); 
                            _isBinOpened = false;
                        }
                        else { OpenThisBin(senderBin); }
                    }
                } 
            });

            BoxSelected = new RelayCommand(() =>
            {
                if(JustReadBin) { return; }
                if(SelectedBox == null) { return; }

                foreach(Fifo fifo in FifoList)
                {
                    foreach(Box box in fifo.DisplayedBoxes)
                    {
                        if(box == SelectedBox)
                        {
                            box.IsSelected = true;
                        }
                        else { box.IsSelected = false; }
                    }
                }
            });

            BoxSelected2 = new RelayCommand(() =>
            {
                if (JustReadPartNumber) { return; }
                if (SelectedBox == null) { return; }

                foreach (Fifo fifo in FifoList)
                {
                    foreach (Box box in fifo.boxes)
                    {
                        if (box == SelectedBox)
                        { 
                            if(box.PartNumber != SelectedPartNumber.PartNumber)
                            {
                                SelectedPartNumber = fifo;
                            }

                            box.IsSelected = true;
                        }
                        else { box.IsSelected = false; }
                    }
                }
            });

            PartNumberSelected = new RelayCommand(() => 
            {
                if (SelectedPartNumber == null) { return; }

                foreach (Fifo fifo in FifoList)
                {
                    if (fifo == SelectedPartNumber)
                    {
                        foreach (Box bx in fifo.boxes)
                        {
                            fifo.DisplayedBoxes.Add(bx);
                        }
                        fifo.BackgroundColor = Color.FromArgb("#ffd573");
                        
                        if(SelectedBox != null)
                        {
                            if (fifo.PartNumber != SelectedBox.PartNumber)
                            {
                                SelectedBox = fifo.boxes.FirstOrDefault();
                            }
                        }
                        else
                        {
                            SelectedBox = fifo.boxes.FirstOrDefault();
                        }   
                    }
                    else
                    {
                        fifo.DisplayedBoxes.Clear();
                        fifo.BackgroundColor = Colors.White;
                    }
                }
            });

            BinSelected = new RelayCommand(() =>
            {
                if(SelectedBin == null) { return; } 

                foreach (Fifo fifo in FifoList)
                {
                    if (fifo == SelectedBin)
                    { 
                        foreach (Box bx in fifo.boxes)
                        {
                            fifo.DisplayedBoxes.Add(bx);
                        }
                        SelectedBin.BackgroundColor = Color.FromArgb("#ffd573");

                        SelectedBox =  SelectedBin.boxes.FirstOrDefault();
                    }
                    else 
                    {
                        fifo.DisplayedBoxes.Clear();
                        fifo.BackgroundColor = Colors.White; 
                    }
                }
            });

            ShowHelpTip = new RelayCommand(() =>
            {
                var page = App.Current.MainPage;
                page.DisplayAlert("Ajuda", "", "Ok");
            });
             
            SearchPartNumber = new RelayCommand(() =>
            {
                SearchText();
            });

            CleanBoxClicked = new RelayCommand(async () =>
            {

                var page = App.Current.MainPage;
                try
                {
                    SelectedBox = getSelectedBoxDisplayed();

                    if (!SelectedBox.PartNumber.IsNullOrEmpty())
                    {
                        // await page.DisplayAlert("Title", "Eliminar esta caixa ? " + SelectedBox.Name + ", Deste PN?" + SelectedBox.PartNumber, ", Deste Bin?" + SelectedBox.Bin, "Ok"); return;
                        
                        if (chkAllBoxes)
                        {
                            bool answerResult = await page.DisplayAlert("Confirmar", $"Pretende Eliminar Todos as caixas no BIN {SelectedBox.Bin} do PN {SelectedBox.PartNumber} ?", "Sim", "Não");
                            if (answerResult)
                            {
                                if (JustReadBin)
                                {
                                    cleanBox(SelectedBox.Bin, SelectedBox.PartNumber, null);
                                    getBinFIFO(PartNumber);
                                }

                                if (JustReadPartNumber)
                                {
                                    cleanBox(SelectedBox.Bin, SelectedBox.PartNumber, null);
                                    getPnFIFO(SelectedBox.Bin);
                                }
                            }
                        }
                        else
                        {
                            bool answerResult = await page.DisplayAlert("Confirmar", $"Pretende Eliminar só a caixa {SelectedBox.Name} no BIN {SelectedBox.Bin} do PN {SelectedBox.PartNumber} ?", "Sim", "Não");
                            if (answerResult)
                            {
                                if (JustReadBin)
                                {
                                    cleanBox(SelectedBox.Bin, SelectedBox.PartNumber, SelectedBox.Name);
                                    getBinFIFO(PartNumber);
                                }

                                if (JustReadPartNumber)
                                {
                                    cleanBox(SelectedBox.Bin, SelectedBox.PartNumber, SelectedBox.Name);
                                    getPnFIFO(SelectedBox.Bin);
                                }
                            }
                        }
                         
                    }
                }
                catch (Exception ex)
                {
                   await page.DisplayAlert("Erro", ex.Message, "Ok");
                }
            });

            CleanBinClicked = new RelayCommand(async() =>
            {
                bool answerResult = await App.Current.MainPage.DisplayAlert("Confirmar", $"Pretende Limpar o BIN {PartNumber}?", "Sim", "Não");
                if(!answerResult) { return; }

                if (JustReadBin)
                {
                    if (!Regex.IsMatch(PartNumber, @"\d{3}-\d{3}")) { return; }
                    cleanBin(PartNumber);
                    getBinFIFO(PartNumber);
                }
                
                if(JustReadPartNumber)
                {
                    if (!Regex.IsMatch(SelectedBin.Bin, @"\d{3}-\d{3}")) { return; }
                    cleanBin(SelectedBin.Bin);
                    getPnFIFO(PartNumber);
                }
            });
        }

        public void OpenThisBin(Fifo binToOpen)
        {
            SelectedBin = null;
            SelectedBin = binToOpen;
            _isBinOpened = true; 
        }

        public void OpenThisPn(Fifo pnToOpen)
        {
            SelectedPartNumber = null;
            SelectedPartNumber = pnToOpen;
            _isPnOpened = true;
        }

        public void ScanTriggered(string message)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (message.StartsWith("A"))
                {
                    string[] codes = message.Split(';');
                    if (codes.Length != 3) throw new Exception("Código 'A' com formato errado");
                    string pn = codes[0].Substring(1, codes[0].Length - 1);
                    PartNumber = pn;
                    return;
                }

                if (message.StartsWith("30S"))
                {
                    PartNumber = message.Substring(3, message.Length - 3);
                    return;
                }

                PartNumber = message;
            });
           
            return;
        }

        private void SearchText()
        {
            SelectedBox = new();
            FifoList.Clear();
            JustReadPartNumber = false;
            JustReadBin = false;

            if (string.IsNullOrEmpty(PartNumber)) return;

            string text = PartNumber;
            switch (text.Substring(0, 1))
            {
                case "A":
                    JustReadPartNumber = true;
                    string[] codes = text.Split(';');
                    if (codes.Length != 3) throw new Exception("Código 'A' com formato errado");
                    PartNumber = codes[0].Substring(1, codes[0].Length - 1);
                    getPnFIFO(PartNumber);
                    break;
                case "P":
                    JustReadPartNumber = true;
                    PartNumber = text.Substring(1, text.Length - 1);
                    getPnFIFO(PartNumber);
                    break;
                default:
                    if (Regex.IsMatch(text, @"\d{3}-\d{3}"))
                    {
                        JustReadBin = true;
                        PartNumber = text;
                        getBinFIFO(text);
                    }
                    else if (text.StartsWith("30S"))
                    {
                        JustReadPartNumber = true;
                        PartNumber = text.Substring(3, text.Length - 3);
                        getPnFIFO(PartNumber);
                    }
                    else
                    {
                        JustReadPartNumber = true;
                        PartNumber = text;
                        if (PartNumber.Length > 12) PartNumber = PartNumber.Substring(0, 8);
                        getPnFIFO(PartNumber);
                    }
                    break;
            }
        } 

        private Box? getSelectedBoxDisplayed()
        {
            foreach(Fifo fifo in FifoList)
            {
                foreach(Box box in fifo.DisplayedBoxes)
                {
                    if(box.IsSelected)
                    {
                        return box;
                    }
                }
            }
            return new();
        }

        private void getPnFIFO(string text)
        {
            _isPnOpened = false;
            _isBinOpened = false;
            FifoList.Clear();
            JustReadPartNumber = true;
            JustReadBin = false;
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
                            Bin = bin
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
                foreach(Fifo orderedFifo in orderedFifoList)
                {
                    orderedFifo.Order = countOrder.ToString() ;
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

        private void cleanBin(string bin)
        {
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("p_wh_clean_bin_r1", cnn);

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@in_bin", SqlDbType.VarChar, 15).Value = bin;
                cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 20).Value = ApplicationSettings.readUser;
                cmd.Parameters.Add("@out_result", SqlDbType.Bit).Direction = ParameterDirection.Output;

                cnn.Open();
                cmd.ExecuteNonQuery();
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

        private void getBinFIFO(string text)
        {
            _isBinOpened = false;
            _isPnOpened = false;
            FifoList.Clear();
            JustReadPartNumber = false;
            JustReadBin = true;
            SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString());
            SqlCommand cmd = new SqlCommand("SELECT Bin, PN, Box, IsFull, BoxType, FIFO FROM v_wh_bin_pn WHERE Bin = @bin ORDER BY FIFO", cnn);
            SqlDataReader reader = null;

            try
            {
                SelectedPartNumber = new();
                SelectedBox = new();

                cmd.Parameters.Add("@bin", SqlDbType.VarChar, 12).Value = text;

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
                    fifoDate = (DateTime)reader["FIFO"];

                    if (pn != prevPn)
                    {
                        // Add previous fifo
                        if(prevPn != null && bin != null)
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
                            PartNumber = pn,
                            Date = fifoDate.ToString("dd/MM/yyyy HH:mm:ss"),
                            Tag= "P" + pn
                        };

                        prevPn = pn;
                        prevBin = bin;
                    }

                    string box = reader["Box"] as string;

                    currentFifo.boxes.Add(new Box()
                    {
                        Name = box,
                        Date = fifoDate.ToString("dd/MM/yyyy HH:mm:ss"),
                        Tag = "B" + box,  
                        Bin = bin,
                        PartNumber = pn, 
                        type = reader["BoxType"].ToString().IsNullOrEmpty() ? "ERR" : reader["BoxType"].ToString()
                    });
                }
                FifoList.Add(currentFifo);

                if(FifoList.Count > 0)
                {
                    SelectedPartNumber = FifoList.FirstOrDefault();
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

        private void cleanBox(string bin, string pn, string box)
        {
            try
            {
                using (SqlConnection cnn = new SqlConnection(new StringBuilder("workstation id=\"Scan\";packet size=4096;user id=")
                    .Append(ApplicationSettings.sUser).Append(";data source=").Append(ApplicationSettings.szIPSelected)
                    .Append(";persist security info=False;TrustServerCertificate=true;initial catalog=").Append(ApplicationSettings.sDB).ToString()))
                {
                    using (SqlCommand cmd = new SqlCommand("p_wh_clean_box_r1", cnn))
                    {

                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@in_bin", SqlDbType.VarChar, 15).Value = bin;
                        cmd.Parameters.Add("@in_pn", SqlDbType.VarChar, 20).Value = pn;
                        cmd.Parameters.Add("@in_box", SqlDbType.VarChar, 20).Value = (object)box ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@in_user", SqlDbType.VarChar, 20).Value = ApplicationSettings.readUser;
                        cmd.Parameters.Add("@out_result", SqlDbType.Bit).Direction = ParameterDirection.Output;

                        cnn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }






    }
}
