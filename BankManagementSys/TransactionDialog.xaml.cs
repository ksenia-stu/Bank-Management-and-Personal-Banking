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

namespace BankManagementSys
{
    /// <summary>
    /// Interaction logic for TransactionDialog.xaml
    /// </summary>
    public partial class TransactionDialog : Window
    {
        User currentUser;
        Account currentAccount;
        string currentTransType;

        public TransactionDialog(User user, Account account, string type)
        {
            InitializeComponent();
            currentTransType = type;
            this.Title = type;
            lblTransacTitle.Content = type;
            btMakeTrans.Content = "Make " +  type;
            if (type == "Transfer")
            {
                lblBenefAcc.Content = "Beneficiary account No:";
                tbBenefAccNo.Visibility = Visibility.Visible;
                btFindBenefAccHolder.Visibility = Visibility.Visible;
            }
            if (type == "Payment")
            {
                lblPayee.Content = "Payee:";
                comboPayees.Visibility = Visibility.Visible;
                try
                {
                    Utilities.Payees = EFData.context.Users.Include("Accounts").Where(u => u.CompanyName != null).ToList();
                }
                catch (SystemException ex)
                {
                    MessageBox.Show("Database error: " + ex.Message, "Database operation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                comboPayees.ItemsSource = Utilities.Payees;
                comboPayees.DisplayMemberPath = "CompanyName";
                lblBenefAccOwner.Content = "Payment category";
                comboPayCategory.Visibility = Visibility.Visible;
                comboPayCategory.ItemsSource = Utilities.paymentCategories;
                comboPayCategory.SelectedIndex = 0;
            }
            currentUser = user;
            currentAccount = account;
            lblOwner.Content = user.FullName;
            lblAccountNo.Content = account.Id;
            lblAccType.Content = account.AccountType.Description;
            lblBalance.Content = account.Balance + " $";

        }

        private bool ValidateFields()
        {
            //all transactions (amount)
            decimal amount;
            try
            {
                amount = decimal.Parse(tbAmount.Text);
                if(amount <= 0)
                {
                    MessageBox.Show("Amount must not be 0 or negative", "Input error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Amount must contain only digits and . symbol", "Input error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (currentTransType != "Deposit" && currentAccount.Balance < amount)
            {
                MessageBox.Show("Insufficient funds to proceed with operation", "Input error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            //transfer (dest account)
            if (currentTransType == "Transfer")
            {
                return ValidateDestAccount();
            }
            //payment (payee account, payment category)
            if (currentTransType == "Payment")
            {
                if (comboPayCategory.Items.Count == 0 || comboPayCategory.SelectedIndex == -1 || comboPayCategory.SelectedIndex == 0)
                {
                    MessageBox.Show("Payment category must be selected", "Input error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                return ValidateDestAccount();
                
            }
            return true;
        }

        private bool ValidateDestAccount()
        {
            int destinationAccNo;
            try
            {
                if (currentTransType == "Transfer")
                {
                    destinationAccNo = int.Parse(tbBenefAccNo.Text);

                    Account beneficiaryAcc = EFData.context.Accounts.SingleOrDefault(a => a.Id == destinationAccNo);
                    if (beneficiaryAcc == null)
                    {
                        lblBenefAccOwner.Content = "This destination account does not exist";
                        lblBenefAccOwner.Foreground = new SolidColorBrush(Colors.Red);
                        return false;
                    }
                    lblBenefAccOwner.Content = string.Format("Beneficiary account holder: {0}", beneficiaryAcc.User.FullName);
                    if(beneficiaryAcc.Id == currentAccount.Id)
                    {
                        MessageBox.Show("Destination account and current account must be different", "Transaction prohibited", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    return true;
                }
                if (currentTransType == "Payment")
                {
                    if (comboPayees.Items.Count == 0 || comboPayees.SelectedIndex == -1)
                    {
                        MessageBox.Show("Payee must be selected", "Input error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    User payee = (User)comboPayees.SelectedItem;

                    Account payeeAcc = (from a in payee.Accounts
                                        where a.AccountType.Description == "Business"
                                        select a).FirstOrDefault();
                    if (payeeAcc == null)
                    {
                        MessageBox.Show("Payee business account does not exist", "Payment impossible", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    return true;
                }
                return false;

            }
            catch (FormatException)
            {
                MessageBox.Show("Destination account must contain digits only", "Input error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (SystemException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Database operation failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }



        //make transaction
        private void MakeTransaction()
        {
            try
            {
                decimal amount = decimal.Parse(tbAmount.Text);
                Transaction transac = new Transaction();
                transac.Date = DateTime.Now;
                transac.Amount = amount;
                transac.Type = currentTransType;
                transac.AccountId = currentAccount.Id;
                if (currentTransType == "Transfer")
                {
                    int destinationAccNo = int.Parse(tbBenefAccNo.Text);
                    transac.ToAccount = destinationAccNo;
                }
                if (currentTransType == "Payment")
                {
                    User payee = (User)comboPayees.SelectedItem;

                    transac.ToAccount = (from a in payee.Accounts
                                         where a.AccountType.Description == "Business"
                                         select a.Id).FirstOrDefault();

                    transac.PaymentCategory = comboPayCategory.Text;
                }
                EFData.context.Transactions.Add(transac);

                decimal previousBalance = currentAccount.Balance; //balance before transaction
                if (currentTransType == "Deposit")
                {
                    currentAccount.Balance = currentAccount.Balance + Math.Round(amount, 2);  //new balance
                }
                if (currentTransType == "Withdrawal")
                {
                    currentAccount.Balance = currentAccount.Balance - Math.Round(amount, 2);  //new balance
                }
                if (currentTransType == "Transfer" || currentTransType == "Payment")
                {
                    Account beneficiaryAcc = EFData.context.Accounts.SingleOrDefault(a => a.Id == transac.ToAccount);
                    currentAccount.Balance = currentAccount.Balance - Math.Round(amount, 2);  //new balance
                    beneficiaryAcc.Balance = beneficiaryAcc.Balance + Math.Round(amount, 2);  //add money to beneficiary
                }
                EFData.context.SaveChanges();
                lblBalance.Content = currentAccount.Balance + " $";


                string message = string.Format("The {0} was completed successfully", currentTransType.ToLower());
                MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                tbAmount.Text = "";
                tbBenefAccNo.Text = "";
                Receipt receiptDlg = new Receipt(currentAccount, previousBalance, transac, currentUser, true);
                receiptDlg.Owner = this;
                bool? result = receiptDlg.ShowDialog();
                if (result == true)
                {
                    MessageBoxResult answer = MessageBox.Show("Would you like to perform another " + currentTransType.ToLower() + " ?", "Choice required", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (answer == MessageBoxResult.No)
                    {
                        DialogResult = true;
                    }
                }
            }
            catch (SystemException ex)
            {
                MessageBox.Show("Database error: " + ex.Message, "Database operation failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void btMakeTrans_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields()) { return; }
            string message = string.Format("Proceed with this {0}: {1} $ ?", currentTransType.ToLower(),
                decimal.Parse(tbAmount.Text).ToString("N2"));
            MessageBoxResult answer = MessageBox.Show(message, "Confirmation required", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes)
            {
                MakeTransaction();
            }
        }

        private void btFindBenefAccHolder_Click(object sender, RoutedEventArgs e)
        {
            ValidateDestAccount();
        }

        private void NumbersOnly(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.NumPad0:
                case Key.NumPad1:
                case Key.NumPad2:
                case Key.NumPad3:
                case Key.NumPad4:
                case Key.NumPad5:
                case Key.NumPad6:
                case Key.NumPad7:
                case Key.NumPad8:
                case Key.NumPad9:
                case Key.D0:
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                    break;
                default:
                    e.Handled = true;
                    break;
            }
        }

        private void MoneyInput(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.NumPad0:
                case Key.NumPad1:
                case Key.NumPad2:
                case Key.NumPad3:
                case Key.NumPad4:
                case Key.NumPad5:
                case Key.NumPad6:
                case Key.NumPad7:
                case Key.NumPad8:
                case Key.NumPad9:
                case Key.D0:
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                case Key.OemPeriod:
                case Key.Decimal:
                    break;
                default:
                    e.Handled = true;
                    break;
            }
        }

        private void tbBeneficiaryAcct_KeyDown(object sender, KeyEventArgs e)
        {
            NumbersOnly(e);
        }

        private void tbAmount_KeyDown(object sender, KeyEventArgs e)
        {
            MoneyInput(e);
        }

    }

}
