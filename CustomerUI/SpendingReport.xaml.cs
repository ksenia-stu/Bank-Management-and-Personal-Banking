﻿using SharedCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CustomerUI
{
    /// <summary>
    /// Interaction logic for SpendingReport.xaml
    /// </summary>
    public partial class SpendingReport : Window
    {
        Account currentAcc;
        public SpendingReport(Account account)
        {
            InitializeComponent();
            currentAcc = account;
            lblAccNumber.Content = currentAcc.Id;
            lblAccType.Content = currentAcc.AccountType.Description;
            lblBalance.Content = currentAcc.Balance;
            //blackout dates before account was open and future dates
            dpFromDate.DisplayDateEnd = DateTime.Now.AddDays(1);
            dpToDate.DisplayDateEnd = DateTime.Now.AddDays(1);
            dpFromDate.DisplayDateStart = currentAcc.OpenDate;
            dpToDate.DisplayDateStart = currentAcc.OpenDate;
            if (currentAcc.CloseDate != null)
            {
                dpFromDate.BlackoutDates.Add(new CalendarDateRange((DateTime)currentAcc.CloseDate, DateTime.MaxValue));
                dpToDate.BlackoutDates.Add(new CalendarDateRange((DateTime)currentAcc.CloseDate, DateTime.MaxValue));
            }
        }

        private void ViewChart()
        {
            if (dpFromDate.SelectedDate == null || dpToDate.SelectedDate == null)
            {
                MessageBox.Show("Both dates must be selected to view data");
                return;
            }
            if(dpToDate.SelectedDate < dpFromDate.SelectedDate)
            {
                MessageBox.Show("'To' date must be later than 'From' date");
                return;
            }
            DateTime fromDate = (DateTime)dpFromDate.SelectedDate;
            DateTime toDate = (DateTime)dpToDate.SelectedDate;
            if (rbPieChart.IsChecked == true)
            {
                this.contentControl.Content = new PieChart(currentAcc, fromDate, toDate);
            }
            else if (rbBarGraph.IsChecked == true)
            {
                this.contentControl.Content = new BarChart(currentAcc, fromDate, toDate);
            }
            else
            {
                this.contentControl.Content = "Choose pie chart or bar graph to visualize data";
            }
        }


        private void btViewReport_Click(object sender, RoutedEventArgs e)
        {
            ViewChart();
        }

        private void rbPieChart_Checked(object sender, RoutedEventArgs e)
        {
            if(this.contentControl == null)
            {
                return;
            }
            ViewChart();
        }

        private void rbBarGraph_Checked(object sender, RoutedEventArgs e)
        {
            ViewChart();
        }
    }
}
