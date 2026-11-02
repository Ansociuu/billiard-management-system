using System;
using billiard_management_system.Core;

namespace billiard_management_system.Models
{
    public class SessionProduct : ObservableObject
    {
        private long _sessionProductId;
        private long _sessionId;
        private int _productId;
        private string _productName;
        private string _unit;
        private int _quantity;
        private decimal _unitPrice;
        private decimal _amount;
        private DateTime _createdAt;

        public long SessionProductId
        {
            get => _sessionProductId;
            set => SetProperty(ref _sessionProductId, value);
        }

        public long SessionId
        {
            get => _sessionId;
            set => SetProperty(ref _sessionId, value);
        }

        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    Amount = _quantity * UnitPrice;
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    Amount = Quantity * _unitPrice;
                }
            }
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }
    }
}

