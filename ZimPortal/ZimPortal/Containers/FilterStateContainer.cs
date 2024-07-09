namespace ZimPortal.Containers
{
    public class FilterStateContainer
    {
        private string? _city;
        public string City
        {
            get => _city ?? string.Empty;
            set
            {
                _city = value;
                NotifyStateChanged();
            }
        }

        private int _distance = 20;

        public int Distance
        {
            get
            {
                return _distance;
            }
            set
            {
                _distance = value;
                NotifyStateChanged();
            }
        }

        private double? _x;
        
        public double X
        {
            get { return _x ?? 0; }
            set
            {
                _x = value;
                NotifyStateChanged();
            }
        }

        private double? _y;

        public double Y
        {
            get { return _y ?? 0; }
            set
            {
                _y = value;
                NotifyStateChanged();
            }
        }

        private bool _open = false;
        public bool Open 
        { 
            get
            {
                return _open;
            } 
            set 
            {
                _open = value;
                NotifyStateChanged();
            }
        }

        private string? _place;

        public string Place
        {
            get { return _place ?? string.Empty; }
            set
            {
                _place = value;
                NotifyStateChanged();
            }
        }

        private List<string> _foodTypes = new List<string>();
        public List<string> FoodTypes()
        {
            return _foodTypes;
        }

        public void AddFoodType(string food)
        {
            _foodTypes.Add(food);
            NotifyStateChanged();
        }

        public void RemoveFoodType(string food)
        {
            _foodTypes.Remove(food);
            NotifyStateChanged();
        }

        // Add delegate method in view to OnChange that calls the blazor "StateHasChanged()" method
        // to rerender the page on state change
        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
