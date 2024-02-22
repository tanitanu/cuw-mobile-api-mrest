using CrystalDecisions.CrystalReports.Engine;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.Serialization.Json;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.UnitedClubPasses;
using ZXing;
using ZXing.Common;

namespace United.Mobile.UnitedClubPasses.Domain
{
    public class Utility
    {
        private readonly IConfiguration _configuration;

        public Utility(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public bool IsUSACountryAddress(MOBCountry country)
        {
            var billingCountryCodes = _configuration.GetSection("BillingCountryCodes").Value ?? "";
            bool isUSAAddress = false;
            if (!string.IsNullOrEmpty(billingCountryCodes) && country != null && !string.IsNullOrEmpty(country.Code))
            {
                var countryCodes = billingCountryCodes.Split('|').ToList();
                isUSAAddress = countryCodes.Exists(p => p.Split('~')[0].ToUpper() == country.Code.Trim().ToUpper());
            }
            else if (!string.IsNullOrEmpty(billingCountryCodes) && country != null && !string.IsNullOrEmpty(country.Name))
            {
                var countryCodes = billingCountryCodes.Split('|').ToList();
                foreach (string coutryCode in countryCodes)
                {
                    if (coutryCode.Split('~')[1].ToUpper() == country.Name.Trim().ToUpper())
                    {
                        isUSAAddress = true;
                        country.Code = coutryCode.Split('~')[0].ToUpper();
                    }
                }
            }
            return isUSAAddress;
        }
        public void SendClubPassReceipt(List<ClubDayPass> passes, string maskedCCNumber)
        {
            try
            {
                using (SmtpClient smtp = new SmtpClient(_configuration.GetSection("EmailServer").Value))
                {
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(_configuration.GetSection("EmailFrom").Value);
                    mail.To.Add(passes[0].email);

                    if (passes.Count == 1)
                    {
                        mail.Subject = "Your United Club one-time pass";
                    }
                    else
                    {
                        mail.Subject = "Your United Club one-time passes";
                    }

                    mail.IsBodyHtml = true;

                    string html = @"
                    
<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Transitional//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd'>
<html xmlns='http://www.w3.org/1999/xhtml'>
<head id='ctl00_Head1'><title>United Airlines</title>
<style type='text/css'>
* {padding:0;margin:0}
table {border-collapse:collapse;border-spacing:0}
img, table {border:0}
.co-message-content p {margin:0 0 12px}
.co-message-content, .co-message-content td, .co-message-content th {font-family:Arial,Helvetica,sans-serif,Tahoma;font-size:12px;color:#333}
.co-message-content ul, .co-message-content ol {margin:12px 0}
.co-message-content li {margin:3px 0 3px 24px}
.co-message-content h4 {color:#039;margin:0 0 10px;font-size:16px}
.co-message-content h5 {color:#000;margin:0;font-size:100%}
</style>
</head>
<body link='#3366cc' vlink='#c3b487' style='margin:0;color:#000;background-color:#dbdbdb'>
<table  border='0' cellspacing='0' cellpadding='0' style='color:#000;background-color:#dbdbdb;width:100%'>
<tr>
<td style='padding:10px'>
<table border='0' cellspacing='0' cellpadding='0' style='width:650px;margin:10px'>
<tr>
<td style='padding-top:10px'>
<table border='0' cellspacing='0' cellpadding='0' style='width:100%'>
<tr id='ctl00_tableDeliveryMsg'>
<td style='color:#666;font-family:Arial,Helvetica,sans-serif,Tahoma;font-size:10px;padding-bottom:5px'>Add <strong>unitedairlines@united.com</strong> to your address book. <a style='color:#666' href='http://www.united.com/safelist' target='_blank'>See instructions.</a></td>
</tr>
</table>
<table border='0' cellspacing='0' cellpadding='0' style='width:100%'>
<tr>
<td style='border:solid 1px #b8b8b8;border-bottom:none;background-color:#36c;height:8px;font-size:6px'>&nbsp;</td>
</tr>
<tr>
<td style='border:solid 1px #b8b8b8;border-top:none;border-bottom:none;background-color:#fff;padding:12px 10px'>
<table cellspacing='0' cellpadding='0' border='0' style='width:100%'>
<tr>
<td><a target='_blank' href='http://www.united.com/'><img src='http://www.united.com/web/format/img/email/template/united-logo.gif' alt='United Airlines' width='191' height='33' /></a></td>
<td style='font-family:Arial,Helvetica,sans-serif,Tahoma;color:#333;font-size:10px;text-align:right;vertical-align:bottom'><div id='ctl00_divCurrentDate'>TODAYS_DATE</div></td>
</tr>
</table>
</td>
</tr>
<tr>
<td style='border:solid 1px #b8b8b8;border-top:none;border-bottom:none;background-color:#fff;padding:10px 10px'>
<table cellspacing='0' cellpadding='0' border='0' style='background-color:#1d3c98;background-image:url(http://www.united.com/web/format/img/email/menu/center.gif);width:100%;height:29px'>
<tr style='font-family:Arial,Helvetica,sans-serif,Tahoma;font-size:12px;color:#fff;font-weight:bold;vertical-align:middle;text-align:center'>
<td style='width:10px;height:29px;font-size:1px'><img src='http://www.united.com/web/format/img/email/menu/left.gif' alt='' width='10' height='29' border='0' /></td>
<td><a href='http://www.united.com/web/en-US/Default.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank' style='color:#fff;text-decoration:none'><span style='color:#fff'>united.com</span></a></td>
<td style='width:5px'>|</td>
<td><a href='http://www.united.com/web/en-US/content/deals/default.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank' style='color:#fff;text-decoration:none'><span style='color:#fff'>Deals &amp; Offers</span></a></td>
<td style='width:5px'>|</td>
<td><a href='http://www.united.com/web/en-US/content/reservations/default.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank' style='color:#fff;text-decoration:none'><span style='color:#fff'>Reservations</span></a></td>
<td style='width:5px'>|</td>
<td><a href='http://www.united.com/web/en-US/content/mileageplus/earn/default.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank' style='color:#fff;text-decoration:none'><span style='color:#fff'>Earn MileagePlus<sup>&reg;</sup> Miles</span></a></td>
<td style='width:5px'>|</td>
<td><a href='http://www.united.com/web/en-US/apps/account/account.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank' style='color:#fff;text-decoration:none'><span style='color:#fff'>My Account</span></a></td>
<td style='text-align:right;width:10px;font-size:1px'><img src='http://www.united.com/web/format/img/email/menu/right.gif' width='10' height='29' border='0' alt=''/></td>
</tr>
</table>
</td>
</tr>
<tr>
<td style='border:solid 1px #b8b8b8;border-top:none;border-bottom:none;background-color:#fff;padding:5px 10px 0'>
<table cellspacing='0' cellpadding='0' border='0' style='width:100%'>
<tr>
<td style='font-family:Arial,Helvetica,sans-serif,Tahoma;color:#006;font-size:18px;line-height:130%;font-weight:bold;padding:0 5px'></td>
</tr>
</table>
</td>
</tr>
<tr>
<td style='border:solid 1px #b8b8b8;border-top:none;border-bottom:none;background-color:#fff;padding:0 10px'>
<table cellspacing='0' cellpadding='0' border='0' style='width:100%'>
<tr>
<td style='font-family:Arial,Helvetica,sans-serif,Tahoma;font-size:12px;line-height:130%;padding-left:5px;color:#333' class='co-message-content'>
<table border='0' cellspacing='0' cellpadding='0' style='width:100%'>
<tr>
<td>
<div style='margin-bottom:2em'>You purchase is complete. YOUR_PASSES_ARE_ATTACHED_IN_THE_EMAIL.</div>
<table border='0' cellspacing='0' cellpadding='0' style='width:100%'>

<tr>
<td style='border:1px solid #002244'>
<table border='0' cellspacing='0' cellpadding='0' style='border-bottom:1px solid #002244;width:100%'>
<tr style='vertical-align:middle'>
<td style='background-color:#002244;padding-left:.35em'><h1 style='color:#FFFFFF;font-size:16px;font-weight:bold;margin:0;background:none;padding:0'>YOUR_PASSES</h1></td>
<td style='background-color:#002244;text-align:center;width:66px;height:30px:align:right'><img src='cid:UnitedClubLogo' width='64' height='28' border='0' alt='ALT_YOUR_PASSES'/></td>
</tr>
</table>
</td>
</tr>

<tr>
<td style='border:1px solid #002244'>
<table border='0' cellspacing='0' cellpadding='0' style='width:100%'>
<tr>
<td>&nbsp;</td>
<tr>
<tr>
<td>PASSES_DETAILS</td>
</tr>
<tr>
    <td>&nbsp;</td>
<tr>
</table>
</td>
</tr>

<tr>
    <td>&nbsp;</td>
<tr>

<tr>
<td style='border:1px solid #666'>
<table border='0' cellspacing='0' cellpadding='0' style='border-bottom:1px solid #666;width:100%'>
<tr style='vertical-align:middle'>
<td style='text-align:center;width:44px;height:30px'><img src='http://www.united.com/web/format/img/icon/dollarCircle.gif' width='26' height='25' border='0' alt='Purchase Summary'/></td>
<td style='background-color:#ccc;padding-left:.35em'><h1 style='color:#039;font-size:14px;font-weight:bold;margin:0;background:none;padding:0'>Purchase Summary</h1></td>
</tr>
</table>

</td>
</tr>
<tr>
<td>

<table border='0' cellspacing='0' cellpadding='0' style='width:100%'>
<tr>
<td style='border:2px solid #69c'>
<table border='0' cellspacing='0' cellpadding='0' style='width:100%'>
<tr><td style='font-size:1px;line-height:100%'><img src='http://www.united.com/web/format/img/email/gradient/bgGradLtBlue630x20Top.gif' width='630' height='20' alt='' style='width:100%' /></td></tr>
<tr>
<td style='padding:12px'>
<table cellpadding='0' cellspacing='0' border='0' style='width:100%'>
<tbody>
<tr>
<td style='text-align:left;padding:0 .25em 0 0'>PURCHASE_SUMMARY</td>
<td style='text-align:right;padding:0 0 0 .25em' colspan='5'>PURCHASE_PRICE</td>
</tr>
<tr><td style='padding:.5em'>&nbsp;</td></tr>
</tbody>
<tbody>
<tr>
<td colspan='5' style='text-align:right;padding:2em 0 2em 1em;border-top:solid 1px #666;font-size:13px'>
<b>Total Price:&nbsp;&nbsp;</b>
</td>
<td style='text-align: right; padding: 2em 0 2em 1em; border-top: solid 1px #666;font-size:13px'>
<b><span id='ctl00_ContentEmail_spanTotalPrice' style='font-size: 1.175em; color: #008800'>TOTAL_PRICE</span></b>
</td>
</tr>
</tbody>
</table>
<p style='margin-bottom:0;padding-top:1em;border-top:solid 2px #69c'><strong>Billing Information</strong></p>
<style type='text/css'>
.paymentDtl th {text-align:left;font-weight:normal;padding-right:1em}
.paymentDtl td {font-weight:bold}
</style>
<table class='paymentDtl'>
<tr id='ctl00_ContentEmail_ucPaymentInfo_trCardName'>
<th>Name of Cardholder:</th>
<td><span id='ctl00_ContentEmail_ucPaymentInfo_spCardName'>CARD_HOLDER_NAME</span></td>
</tr>
<tr id='ctl00_ContentEmail_ucPaymentInfo_trCardType'>
<!--th>Card Type:</th>
<td><span id='ctl00_ContentEmail_ucPaymentInfo_spCardType'>CARD_TYPE</span></td-->
</tr>
<tr id='ctl00_ContentEmail_ucPaymentInfo_trCardNumber'>
<th>Card Number:</th>
<td><span id='ctl00_ContentEmail_ucPaymentInfo_spCardNumber'>CARD_NUMBER</span></td>
</tr>
</table>
</td>
</tr>
<tr>
<td style='font-size:1px;line-height:100%'><img src='http://www.united.com/web/format/img/email/gradient/bgGradLtBlue630x20Btm.gif' width='630' height='20' alt='' style='width:100%' />
</td>
</tr>
</table>
</td>
</tr>
</table>
</td>
</tr>
</table>
</td>
</tr>
<tr>
<td>&nbsp;</td>
</tr>
</table>
</td>
</tr>
</table>
</td>
</tr>
<tr>
<td style='padding-top:17px'>
<table border='0' cellspacing='0' cellpadding='0' style='width:100%'>
<tr style='vertical-align:top'>
<td style='font-family:Arial,Helvetica,sans-serif,Tahoma;font-size:10px;width:350px;color:#666'>View our <a href='http://www.united.com/web/en-US/content/privacy.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank' style='color:#666'>Privacy Policy</a>.</td>
<td style='width:300px;text-align:right'><a href='http://www.united.com/web/en-US/content/company/alliance/star.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank'><img src='http://www.united.com/web/format/img/email/template/staralliance.gif' width='190' height='20' border='0' alt='Star Alliance' /></a></td>
</tr>
</table>
</td>
</tr>
<tr>
<td style='padding-top:17px;'> 
<table style='font-family:Arial,Helvetica,sans-serif;font-size:10px;color:#333;margin:0 width=100%'>
<tr>
<td style='padding:0 3px'>Find United on:</td>
<td style='padding:0 3px'><a href='http://www.united.com/web/en-US/apps/vendors/out.aspx?sender=TECH&camp=&campyear=2012&Language=en-US&i=emlfacebook' target='_blank'><img src='http://www.united.com/web/format/img/email/template/icon-facebook.png' width='16' height='16' border='0' alt='Facebook' /></a></td>
<td style='padding:0 3px'><a href='http://www.united.com/web/en-US/apps/vendors/out.aspx?sender=TECH&camp=&campyear=2012&Language=en-US&i=emltwitter' target='_blank'><img src='http://www.united.com/web/format/img/email/template/icon-twitter.png' width='16' height='16' border='0' alt='Twitter' /></a></td>
</tr>
</table>
</td>
</tr>
<tr>
<td>&nbsp;</td>
</tr>
<tr>
<td style='padding:2px'>
<table border='0' cellspacing='0' cellpadding='0' style='width:100%'>
<tr>
<td style='font-family:Arial,Helvetica,sans-serif,Tahoma;color:#666;font-size:12px;line-height:130%'>
<div><strong>E-mail Information</strong></div>
<div><strong>Please do not reply to this message using the &#34reply&#34 address.</strong></div><br />

<div>If you are experiencing technical issues, please contact the Electronic Support Desk by <a href='http://www.united.com/web/en-US/content/Contact/technical/support.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank'>e-mail</a> or <a href='http://www.united.com/web/en-US/content/Contact/technical/default.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank'>telephone</a>. For issue related to your MileagePlus account, please contact the MileagePlus Center by <a href='http://www.united.com/web/en-US/content/Contact/technical/support.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank'>e-mail</a> or <a href='http://www.united.com/web/en-US/content/Contact/technical/default.aspx?sender=TECH&camp=&campyear=2012&Language=en-US' target='_blank'>telephone</a>.</div><br />

<div>The information contained in this e-mail is intended for the original recipient only.</div><br />

</td>
</tr>
</table>

</td>
</tr>
<tr>
<td style='padding:5px 0'>

<table cellspacing='0' cellpadding='0' border='0' style='width:100%'>
<tr>
<td style='font-family:Arial,Helvetica,sans-serif,Tahoma;color:#666;font-size:10px'>
Copyright &copy; 2012 United Air Lines, Inc.
</td>
</tr>
</table>
</td>
</tr>
</table>
</td>
</tr>
</table>

</body>
</html>
                    ";

                    html = html.Replace("TODAYS_DATE", DateTime.Today.ToString("dddd, MMMM dd, yyyy"));
                    if (passes.Count == 1)
                    {
                        html = html.Replace("YOUR_PASSES_ARE_ATTACHED_IN_THE_EMAIL", "Your pass is attached to this email");
                        html = html.Replace("YOUR_PASSES", "Your Pass");
                        html = html.Replace("ALT_YOUR_PASSES", "Your Pass");
                    }
                    else
                    {
                        html = html.Replace("YOUR_PASSES_ARE_ATTACHED_IN_THE_EMAIL", "Your passes are attached to this email");
                        html = html.Replace("YOUR_PASSES", "Your Passes");
                        html = html.Replace("ALT_YOUR_PASSES", "Your Passes");
                    }

                    StringBuilder passDetails = new StringBuilder();
                    foreach (var pass in passes)
                    {
                        passDetails.Append("<tr>");
                        passDetails.Append("<td style='text-align:left;padding:0 .25em 0 .5em'><p style='color:#333333;font-size:12px;margin:3;background:none;padding:0'>");
                        passDetails.Append("United Club one-time pass number");
                        passDetails.Append("</p></td>");
                        passDetails.Append("<td><p style='color:#333333;font-size:12px;margin:5;background:none;padding:0'>");
                        passDetails.Append(pass.passCode);
                        passDetails.Append("</p></td>");
                        passDetails.Append("<td><p style='color:#333333;font-size:12px;margin:5;background:none;padding:0'>");
                        passDetails.Append("Valid through &nbsp;");
                        passDetails.Append(pass.expirationDate);
                        passDetails.Append("</p></td>");
                        passDetails.Append("</tr>");
                    }
                    html = html.Replace("PASSES_DETAILS", passDetails.ToString());

                    string purchaseSummary = string.Empty;
                    string totalPrice = string.Empty;
                    if (passes.Count == 1)
                    {
                        purchaseSummary = ConvertNumberToNumberName(passes.Count) + " United Club one-time pass at $" + passes[0].paymentAmount + " per pass";
                        totalPrice = String.Format("{0:C}", passes[0].paymentAmount * passes.Count);
                    }
                    else
                    {
                        purchaseSummary = ConvertNumberToNumberName(passes.Count) + " United Club one-time passes at $" + passes[0].paymentAmount + " per pass";
                        totalPrice = String.Format("{0:C}", passes[0].paymentAmount * passes.Count);
                    }

                    html = html.Replace("PURCHASE_SUMMARY", purchaseSummary);
                    html = html.Replace("PURCHASE_PRICE", totalPrice);
                    html = html.Replace("TOTAL_PRICE", totalPrice);

                    html = html.Replace("CARD_HOLDER_NAME", passes[0].firstName + " " + passes[0].lastName);
                    html = html.Replace("CARD_NUMBER", maskedCCNumber);

                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(html, null, "text/html");

                    LinkedResource unitedClubLogo = new LinkedResource(string.Empty);
                    unitedClubLogo.ContentId = "UnitedClubLogo";
                    htmlView.LinkedResources.Add(unitedClubLogo);

                    mail.AlternateViews.Add(htmlView);

                    for (int i = 0; i < passes.Count; ++i)
                    {
                        mail.Attachments.Add(new Attachment(GetClubPassPDF(passes, i)));
                    }
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }
            }
            catch (System.Exception ex)
            {

            }
        }
        internal string GetClubPassPDF(List<ClubDayPass> passes, int passIndex)
        {
            string fullFileName = string.Empty;

            DataSet dataSet = new DataSet();
            dataSet.Tables.Add();

            DataTable dataTable = new DataTable("Pass");

            dataTable.Columns.Add("PassCode", typeof(string));
            dataTable.Columns.Add("MPAccountNumber", typeof(string));
            dataTable.Columns.Add("FirstName", typeof(string));
            dataTable.Columns.Add("LastName", typeof(string));
            dataTable.Columns.Add("Email", typeof(string));
            dataTable.Columns.Add("PaymentAmount", typeof(double));
            dataTable.Columns.Add("PurchaseDate", typeof(string));
            dataTable.Columns.Add("ExpirationDate", typeof(string));
            dataTable.Columns.Add("BarCode", typeof(byte[]));

            int index = 0;
            foreach (var pass in passes)
            {
                if (index == passIndex)
                {
                    DataRow newRow = dataTable.NewRow();
                    newRow["PassCode"] = pass.passCode;
                    newRow["MPAccountNumber"] = pass.mileagePlusNumber;
                    newRow["FirstName"] = pass.firstName;
                    newRow["LastName"] = pass.lastName;
                    newRow["Email"] = pass.email;
                    newRow["PaymentAmount"] = pass.paymentAmount;
                    newRow["PurchaseDate"] = pass.purchaseDate;
                    newRow["ExpirationDate"] = pass.expirationDate;
                    newRow["BarCode"] = pass.barCode;
                    dataTable.Rows.Add(newRow);
                }
                ++index;
            }

            dataSet.Tables.Add(dataTable);

            ReportDocument reportDocument = null;
            try
            {
                reportDocument.Export();
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            finally
            {
                try
                {
                    if (reportDocument != null)
                    {
                        reportDocument.Close();
                    }
                }
                catch (System.Exception) { }
                try
                {
                    if (reportDocument != null)
                    {
                        reportDocument.Dispose();
                    }
                }
                catch (System.Exception) { }

                reportDocument = null;
            }

            return fullFileName;
        }
        internal string ConvertNumberToNumberName(int number)
        {
            string numberName = number.ToString();

            switch (number)
            {
                case 1:
                    numberName = "One";
                    break;
                case 2:
                    numberName = "Two";
                    break;
                case 3:
                    numberName = "Three";
                    break;
                case 4:
                    numberName = "Four";
                    break;
                case 5:
                    numberName = "Five";
                    break;
                case 6:
                    numberName = "Six";
                    break;
                case 7:
                    numberName = "Seven";
                    break;
                case 8:
                    numberName = "Eight";
                    break;
                case 9:
                    numberName = "Nine";
                    break;
                case 10:
                    numberName = "Ten";
                    break;
                case 11:
                    numberName = "Eleven";
                    break;
                case 12:
                    numberName = "Twelve";
                    break;
                case 13:
                    numberName = "Thirteen";
                    break;
                case 14:
                    numberName = "Fourteen";
                    break;
                case 15:
                    numberName = "Fifteen";
                    break;
                case 16:
                    numberName = "Sixteen";
                    break;
                case 17:
                    numberName = "Seventeen";
                    break;
                case 18:
                    numberName = "Eighteen";
                    break;
                case 19:
                    numberName = "Nineteen";
                    break;
                case 20:
                    numberName = "Twenty";
                    break;
            }

            return numberName;
        }
        public string JsonSerialize<T>(T t)
        {
            try
            {
                DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeof(T));
                MemoryStream memoryStream = new MemoryStream();
                dataContractJsonSerializer.WriteObject(memoryStream, t);
                string jsonString = Encoding.UTF8.GetString(memoryStream.ToArray());
                memoryStream.Close();
                return jsonString;
            }
            catch
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(t);
            }
        }
        public byte[] GetBarCode(string data)
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            ZXing.BarcodeWriter writer = new ZXing.BarcodeWriter()
            {
                Format = BarcodeFormat.PDF_417,
                Options = new EncodingOptions
                {
                    Height = 200,
                    Width = 800,
                    PureBarcode = false,
                    Margin = 10
                },
            };
            System.Drawing.Bitmap bitmap = writer.Write(data);
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
            return stream.ToArray();
        }
        public T DeserializeJsonDataContract<T>(string json)
        {
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                var obj = (T)serializer.ReadObject(ms);
                return obj;
            }
        }
    }
}
