using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestFDRReportReader
{
    public partial class DashboardViewer : DevExpress.XtraBars.TabForm
    {
        public DashboardViewer()
        {
            InitializeComponent();
            System.IO.MemoryStream mem = new System.IO.MemoryStream();
            String url = "http://185.116.160.97:8082/Files/FlightDatabyACReg.xml";
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            dashboardViewer1.LoadDashboard(dataStream);

            
            String url2 = "http://185.116.160.97:8082/Files/FlightDatabyCrew.xml";
            WebRequest request2 = WebRequest.Create(url2);
            WebResponse response2 = request2.GetResponse();
            Stream dataStream2 = response2.GetResponseStream();
            dashboardViewer2.LoadDashboard(dataStream2);

            String url3 = "http://185.116.160.97:8082/Files/FlightDatabyGrid.xml";
            WebRequest request3 = WebRequest.Create(url3);
            WebResponse response3 = request3.GetResponse();
            Stream dataStream3 = response3.GetResponseStream();
            dashboardViewer3.LoadDashboard(dataStream3);

            String url4 = "http://185.116.160.97:8082/Files/FlightDatabyOccuranceNo.xml";
            WebRequest request4 = WebRequest.Create(url4);
            WebResponse response4 = request4.GetResponse();
            Stream dataStream4 = response4.GetResponseStream();
            dashboardViewer4.LoadDashboard(dataStream4);
            //dashboardViewer1.LoadDashboard("185.116.160.97:8082\\Files\\FlightData.xml");
            //dashboardViewer2.LoadDashboard("185.116.160.97:8082\\Files\\FlightDatabyCrew.xml");
        }
        private void dashboardViewer1_CustomizeDashboardTitleText(object sender, DevExpress.DashboardWin.CustomizeDashboardTitleTextEventArgs e)
        {
            string dashboardItemName = "";
            dashboardViewer1.GetCurrentSelection(dashboardItemName);
            e.Text = dashboardViewer1.Dashboard.Title.Text + " " + dashboardItemName;//dashboardViewer1.Parameters[0].Value;
        }

    }
}
