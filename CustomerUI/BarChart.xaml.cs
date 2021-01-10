﻿using SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CustomerUI
{
    /// <summary>
    /// Interaction logic for BarChart.xaml
    /// </summary>
    public partial class BarChart : UserControl
    {
        Account currentAcc;
        public BarChart(Account account)
        {
            InitializeComponent();
            currentAcc = account;
            LoadBarChartData();
        }

        private void LoadBarChartData()
        {
            //load all payments
            List<Transaction> transactions = EFData.context.Transactions.Where(t => t.AccountId == currentAcc.Id && t.PaymentCategory != null).ToList();
            //FIX exception

            if (transactions.Count == 0)
            {
                MessageBox.Show("There are no payements over this period of time");
                return;
            }

            List<decimal> amounts = new List<decimal>();

            decimal sum = 0;
            foreach (string pc in Utils.paymentCategories)
            {
                var transacByCat = transactions.FindAll(t => t.PaymentCategory == pc);

                foreach (Transaction t in transactions)
                {
                    sum = sum + t.Amount;
                }
                amounts.Add(sum);
            }

            var CategoryAmount = new List<KeyValuePair<string, decimal>>();

            CategoryAmount = Enumerable.Range(0, Utils.paymentCategories.Count)
            .Select(i => new KeyValuePair<string, decimal>(Utils.paymentCategories[i], amounts[i]))
            .ToList();
            ((BarSeries)mcChart.Series[0]).ItemsSource = CategoryAmount;
        }     
    }
}