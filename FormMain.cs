using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Data.SqlClient;



namespace TestFDRReportReader
{
    public partial class FormMain : Form
    {
        FolderBrowserDialog folder = new FolderBrowserDialog();
        
        string Connection="Data Source=185.116.160.97,2014;Initial Catalog=FDRHH;User ID=FDM;Password=fdmhh";

        public FormMain()
        {
            InitializeComponent();
        }

        private void buttonSelectPath_Click(object sender, EventArgs e)
        {
            listBoxFilelist.Items.Clear();
            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int i, j;
                String[] filename;
                j = folder.SelectedPath.Length;
                filename = Directory.GetFiles(folder.SelectedPath, "*.htm");
                for (i = 0; i < filename.Length; i++)
                    listBoxFilelist.Items.Add(filename[i].Substring(j + 1));
            }
        }

        private void listBoxFilelist_SelectedIndexChanged(object sender, EventArgs e)
        {
            webBrowserRead.Url = new Uri(folder.SelectedPath + "/" + listBoxFilelist.SelectedItem);
        }

        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            string SaveResult = "";

            int i = 0, L = listBoxFilelist.Items.Count;
            progressBar1.Value = 0;
            progressBar1.Maximum = L;
            foreach (var item in listBoxFilelist.Items)
            {
                i++;
                progressBar1.Value = i;
                string Log = "", ACReg = "", FltNo="", DepStn = "", ArrStn = "", DateUTC = "";
                //textBox1.Text += item + Environment.NewLine;

                string URL = "file://" + folder.SelectedPath + @"\" + item;
                WebBrowserLoadCompleted = false;
                webBrowserRead.Stop();
                webBrowserRead.Navigate(URL);
                int d = 0;
                while ((WebBrowserLoadCompleted == false) && (++d < 300))
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(10);
                }
                XmlDocument xml = new XmlDocument();
                XmlDocument xmlTech = new XmlDocument();
                //Find ACReg
                ACReg = FindRegDateLeg(webBrowserRead.Document, "ACReg");
                this.Text += item + "->"+ACReg+"    ";

                //Find FltNo
                FltNo = FindRegDateLeg(webBrowserRead.Document, "FltNo");
                
                //Find DepStn
                DepStn = FindDepArr(webBrowserRead.Document, "tr", "Departure Airport");

                //Find ArrStn
                ArrStn = FindDepArr(webBrowserRead.Document, "tr", "Airport of Landing");

                //Find DateUTC
                if (ACReg == "EP-TBJ")
                {
                    DateUTC = FindRegDateLeg(webBrowserRead.Document, "DateUTCTBJ");
                }
                else
                {
                    DateUTC = FindRegDateLeg(webBrowserRead.Document, "DateUTC");
                    DateTime Date = ConvertStrtoDateTime(DateUTC);
                    if (Date.Year == 2000)
                    {
                        Date = Date.AddYears(20);
                       DateUTC = Date.ToString("dd.MM.yyyy HH:mm");
                    }
                    /*
                    String Year = DateUTC.Substring(6, 4);
                        if (ACReg == "EP-TBF" && Year == "2000")
                    {
                        DateUTC = DateUTC.Replace("2000", "2020");
                    }
                    */
                }
                //Find  "Piloting Occurances" Table
                HtmlElement table_pilotingOccurances = FindTable(webBrowserRead.Document, "b", "piloting occurrences");
                if (table_pilotingOccurances == null)
                {
                    Log += "piloting Occurances Table not found" + Environment.NewLine;
                }

                //Extract XML Data
                XmlElement root = xml.CreateElement("Occurances");
                xml.AppendChild(root);
                ScanSiblingTables(table_pilotingOccurances, root, xml, "Occurance");
                //textBox1.Text += xml.OuterXml + Environment.NewLine;

                //Find  "Technological messages" Table
                HtmlElement table_Technologicalmessages = FindTable(webBrowserRead.Document, "b", "Technological messages");
                if (table_Technologicalmessages == null)
                {
                    Log += "Technological messages Table not found" + Environment.NewLine;
                }
                //Extract XML Data
                root = xmlTech.CreateElement("Technologicals");
                xmlTech.AppendChild(root);
                ScanSiblingTables(table_Technologicalmessages, root, xmlTech, "Technological");

                //Save Data To DB
                String HTML = File.ReadAllText(folder.SelectedPath + @"\" + item);
                SaveResult += Environment.NewLine + SaveToDB(DateUTC, DepStn, ArrStn, ACReg, FltNo, xml, HTML,xmlTech); 
                Application.DoEvents();
            } 
            MessageBox.Show(SaveResult);
        }

        private string SaveToDB(string DateUTC, string DepStn, string ArrStn, string ACReg
            , string FltNo, XmlDocument xml, String HTML, XmlDocument xmlTech)
        {
            string Result = "Not Saved";
            DateTime Date = ConvertStrtoDateTime(DateUTC);
            string sqlins = "Execute Update_FlightData @Date, @DepStn, @ArrStn, @ACReg, @FltNo, @OccurencesXML, @OccurencesHTML, @xmlTech";
            
            using (SqlConnection Connections = new SqlConnection(Connection))
            {
                SqlCommand comand = new SqlCommand(sqlins, Connections);
                {
                    int i=comand.Parameters.Count;
                    comand.Parameters.Add("Date",SqlDbType.Date).Value=Date;
                    comand.Parameters.Add("DepStn",SqlDbType.VarChar).Value =DepStn;
                    comand.Parameters.Add("ArrStn", SqlDbType.VarChar).Value =ArrStn;
                    comand.Parameters.Add("ACReg", SqlDbType.VarChar).Value =ACReg;
                    comand.Parameters.Add("FltNo", SqlDbType.VarChar).Value = FltNo;
                    comand.Parameters.Add("OccurencesXML", SqlDbType.VarChar).Value = xml.OuterXml;
                    comand.Parameters.Add("OccurencesHTML", SqlDbType.VarChar).Value = HTML;
                    comand.Parameters.Add("xmlTech", SqlDbType.VarChar).Value = xmlTech.OuterXml;
                    try
                    {
                        Connections.Open();
                        SqlDataReader reader = comand.ExecuteReader();
                        int Updated = 0;
                        int Inserted = 0;
                        if (reader.Read())
                        {
                            Updated = reader.GetSqlInt32(0).Value;
                            Inserted = reader.GetSqlInt32(1).Value;
                        }
                        Result = ACReg+" "+DateUTC.ToString() + ":" + "Updated:" + Updated.ToString() + " Inserted:" + Inserted.ToString();
                    }
                    catch (Exception ex)
                    {
                        Result = "Not Save:" + ex.Message;
                    }
                }
            }
            return Result;
        }
       
        private DateTime ConvertStrtoDateTime(string dateUTC)
        {
            int y, m, d; string Str = "";
            dateUTC =dateUTC.TrimStart().TrimEnd();
            d = dateUTC.IndexOf('.');
            Str=dateUTC.Substring(0, d);
            dateUTC = dateUTC.Substring(d + 1);
            d = Convert.ToInt16(Str);
            m = dateUTC.IndexOf('.');
            Str = dateUTC.Substring(0, m);
            dateUTC = dateUTC.Substring(m + 1);
            m = Convert.ToInt16(Str);
            y = dateUTC.IndexOf('.');
            Str = dateUTC.Substring(0,4 );
            y = Convert.ToInt16(Str);
            return new DateTime(y, m, d);
            //dateUTC = dateUTC.Substring(m + 1);
        }

        public string FindRegDateLeg(HtmlDocument doc,string tag)
        {
            HtmlElement innerElement = null; int i=0 , L= 0, j=0; int start = -1; string ACReg = "", DateUTC = "", FltNo = "";
            foreach (HtmlElement element in doc.All)
            {
                if (element.TagName.ToLower() == "table")
                {
                    innerElement = FindChildTag(element, "p", "id ");
                    if (innerElement != null)
                    {
                        if (innerElement.InnerText != null)
                        {
                            DateUTC = ACReg=FltNo = innerElement.InnerText.Trim();
                            //ACReg
                            if (tag == "ACReg")
                            {
                                start = ACReg.IndexOf("ID ");
                                if (start >= 0)
                                    ACReg = ACReg.Substring(start + 3, 6);
                                return ACReg;
                            }

                            //DateUTC
                            if (tag == "DateUTC")
                            {
                                start = DateUTC.IndexOf("Flight");
                                if (start >= 0)
                                {
                                    DateUTC = DateUTC.Substring(start);
                                    L = DateUTC.Length;
                                    for (i = 0; i < L; i++)
                                    {
                                        if (DateUTC[i] == ' ')
                                        {
                                            j++;
                                            if (j > 5)
                                            {
                                                //if (DateUTC.Substring(i, L - i).TrimStart().TrimEnd()=="")
                                                return DateUTC.Substring(i, L - i).TrimStart().TrimEnd();
                                            }
                                        }
                                    }
                                }
                            }
                            if (tag == "DateUTCTBJ")
                            {
                                start = DateUTC.IndexOf("Flight");
                                if (start >= 0)
                                {
                                    DateUTC = DateUTC.Substring(start);
                                    L = DateUTC.Length;
                                    for (i = 0; i < L; i++)
                                    {
                                        if (DateUTC[i] == ' ')
                                        {
                                            j++;
                                            if (j >= 7)
                                            {
                                                return DateUTC.Substring(i, L - i).TrimStart().TrimEnd();
                                            }
                                        }
                                    }
                                }
                            }
                            //FltNo
                            if (tag == "FltNo")
                            {
                                start = FltNo.IndexOf("Flight");
                                if (start >= 0)
                                {
                                    i = 0;
                                    i = FltNo.Substring(start + 7).IndexOf(" ");
                                    FltNo = FltNo.Substring(start+7,i);
                                    return FltNo.TrimStart().TrimEnd();
                                       
                                }
                            }
                        }  
                    }
                }
           
            }
            return null;
        }

        public string FindDepArr(HtmlDocument doc,string TokenTagName, string TagDataText)
        {
            foreach (HtmlElement element in doc.All)
            {
                if (element.TagName.ToLower() == "table")
                {
                    if (CheckForDataExist(element, TokenTagName, TagDataText))
                    {
                        HtmlElement elementTemp = FindTable(doc, TokenTagName, TagDataText);
                        if (elementTemp != null)
                        {
                            if (elementTemp.InnerText != null)
                            {
                                int i = elementTemp.InnerText.IndexOf("ICAO Code");
                                return elementTemp.InnerText.Substring(i + 9, 4);
                                    
                            }
                        }
                        
                    }
                }
            }
            return null;
        }

        public HtmlElement FindChildTag(HtmlElement tag, string TokenTagName, string TagDataText)
        {
            foreach (HtmlElement element in tag.All)
            {
                if (element.TagName.ToLower() == TokenTagName)
                {
                    if (element.InnerText != null)
                    {
                        if (element.InnerText.ToLower().Trim().Contains(TagDataText.ToLower()))
                            return element;
                    }
                }
            }
            return null;
        }

        public HtmlElement FindTable(HtmlDocument doc, string TokenTagName, string TagDataText)
        {
            foreach (HtmlElement element in doc.All)
            {
                if (element.TagName.ToLower() == "table")
                {
                    if (CheckForDataExist(element, TokenTagName, TagDataText))// "b", "piloting occurrences"))
                    {
                        if (!HasChildTable(element))
                            return element;
                    }
                }
            }
            return null;
        }

        public bool CheckForDataExist(HtmlElement tag, string TokenTagName, string TagDataText)
        {
            foreach (HtmlElement element in tag.All)
            {
                if (element.TagName.ToLower() == TokenTagName)
                {
                    if (element.InnerText != null)
                    {
                        if (element.InnerText.ToLower().Trim().Contains(TagDataText.ToLower()))
                            return true;
                    }
                }
            }
            return false;
        }

        public bool HasChildTable(HtmlElement element)
        {
            foreach (HtmlElement child in element.All)
            {
                if (child.TagName.ToLower() == "table")
                    return true;
            }
            return false;
        }

        public List<String> ExtractTableRowData(HtmlElement tableRow)
        {
            String Row = "";int i = 0;
            List<String> tt = new List<String>();
            foreach (HtmlElement cell in tableRow.All)
            {
                if (cell.TagName.ToUpper() == "TT")
                {
                    Row += cell.InnerText+", ";
                    tt.Add(cell.InnerText);
                }
            }
            return tt;
        }

        public void ScanSiblingTables(HtmlElement table, XmlElement root, XmlDocument xml, string name)
        {
            if (table == null)
                return;

            HtmlElement siblingTable = table.NextSibling;
            int i = 0;
            Dictionary<String, String> acc = new Dictionary<String, String>();
            while (siblingTable != null)
            {
                if (siblingTable.TagName.ToUpper() == "TABLE")
                    ExtractTableData(siblingTable, siblingTable.NextSibling, xml, root, name);
                siblingTable = siblingTable.NextSibling;
                if (siblingTable != null)
                    siblingTable = siblingTable.NextSibling.NextSibling;
            }
        }

        public void ExtractTableData(HtmlElement table1, HtmlElement table2
                                   , XmlDocument xml, XmlElement Occurances, string name)
        {
            int tr = 0;
            List<String> tt1 = new List<string>(); List<String> tt2 = new List<String>();
            List<String> tttitle = new List<String>();
            

            foreach (HtmlElement el in table1.All)
                if (el.TagName.ToUpper() == "TR")
                    tttitle = ExtractTableRowData(el);
            
            foreach (HtmlElement el in table2.All)
                if (el.TagName.ToUpper() == "TR")
                {
                    tr++;
                    if (tr == 1)
                        tt1 = ExtractTableRowData(el);
                    if (tr >= 2)
                    {
                        tt2 = ExtractTableRowData(el);
                        AddChildtoXML(tttitle, tt1, tt2, xml, Occurances, name);
                    }
                }
            
        }
        private void AddChildtoXML(List<string> tttitle, List<string> tt1, List<string> tt2
                                 , XmlDocument xml, XmlElement Occurances, string name)
        {
            int i = 0;int L = 0;string Title = "";string No = "";
            Dictionary<String, String> final = new Dictionary<String, string>();

            if (tttitle.Count > 0)
            {
                final.Add("No", tttitle[0]);
                L = tttitle[1].IndexOf("/");
                if (L > 0)
                {
                    Title = tttitle[1].Substring(0, L - 1);
                    No = tttitle[1].Substring(L+1);
                }
                else Title = tttitle[1];
                final.Add("Title", Title);

                L = No.IndexOf("/");
                if ((L > 0) & (name == "Occurance"))
                {
                    No = No.Substring(0, L);
                    final.Add("OccuranceNo", No);
                }
                else if (name == "Occurance")
                    final.Add("OccuranceNo", "");
                     
            }
            for (i = 0; i < tt1.Count(); i++)
                if (tt1.Count > 0 & tt2.Count > 0)
                {
                    if (tt1[i] == " ") tt1[i] = "Info";
                    final.Add(tt1[i], tt2[i]);
                }
            XmlElement Occurance;
            if (name == "Occurance")
                Occurance = xml.CreateElement("Occurance");
            else 
                Occurance = xml.CreateElement("Technological");
            foreach (var item in final)
            {
                if (item.Key == "&nbsp")
                    Occurance.SetAttribute("Info", item.Value);
                else
                    Occurance.SetAttribute(item.Key, item.Value);
            }
            Occurances.AppendChild(Occurance);
        }


        Boolean WebBrowserLoadCompleted = false;
        private void webBrowserRead_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            WebBrowserLoadCompleted = true;
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            
            DateTime Start = new DateTime(dateTimePickerStart.Value.Year, dateTimePickerStart.Value.Month, dateTimePickerStart.Value.Day);
            DateTime Finish = new DateTime(dateTimePickerFinish.Value.Year, dateTimePickerFinish.Value.Month, dateTimePickerFinish.Value.Day);
            string sql = "Select * from FlightData Where DateUTC>='"+Start.ToString("yyyy-MM-dd")+"' and DateUTC<='"+Finish.ToString("yyyy-MM-dd") + "'";
            SqlConnection con = new SqlConnection(Connection);
            SqlDataAdapter adapter = new SqlDataAdapter(sql, con);
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            con.Open();
            adapter.Fill(ds,"FlightData");
            con.Close();
            dataGridViewTable.DataSource = ds;
            dataGridViewTable.DataMember = "FlightData";
            dataGridViewTable.Columns[0].ReadOnly = true;
            dataGridViewTable.Columns[1].ReadOnly = true;
            dataGridViewTable.Columns[2].ReadOnly = true;
            dataGridViewTable.Columns[3].ReadOnly = true;
            dataGridViewTable.Columns[4].ReadOnly = true;
            //dataGridViewTable.Columns[5].ReadOnly = true;
            //dataGridViewTable.Columns[6].ReadOnly = true;
        }


        private void dataGridViewTable_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
           

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //DashboardDesigner dash = new DashboardDesigner();
            DashboardViewer dash = new DashboardViewer();
            dash.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DashboardDesigner dash = new DashboardDesigner();
            dash.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ReportDesigner rep = new ReportDesigner();
            rep.Show();
        }
    }
}
