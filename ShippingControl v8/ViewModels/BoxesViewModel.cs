using ShippingControl_v8.Commands;
using ShippingControl_v8.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ShippingControl_v8.ViewModels
{
    public class BoxesViewModel : ViewModelBase
    {
        public ICommand CloseModal { get; }
        public ICommand ConfirmModal { get; }
        public ICommand DeleteBox { get; }
        public ICommand deletebox2 { get; }
        public bool WasModalConfirmed { get; set; } = false;
        public ObservableCollection<BoxQty> boxes { get; set; } = new();
        public BoxesViewModel(INavigation navigation, ObservableCollection<BoxQty> refBoxes) 
        {
            boxes = refBoxes;

            CloseModal = new RelayCommand(async () =>
            {
                WasModalConfirmed = false;
                await navigation.PopModalAsync(true);
            }); 

            ConfirmModal = new RelayCommand(async () =>
            {
                WasModalConfirmed = true;
                await navigation.PopModalAsync(true);
            });

            DeleteBox = new RelayCommand(() =>
            {
               BoxQty boxToDelete = new("","",0);

               foreach (BoxQty box in boxes)
               {
                    if(box.BackgroundColor != Colors.Transparent)
                        boxToDelete = box;
               }
                boxes.Remove(boxToDelete);
                OnPropertyChanged(nameof(boxes));
            });
        }
    }
}
