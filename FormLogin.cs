using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestFDRReportReader
{
    public partial class FormLogin : Form
    {
        public FormLogin()
        {
            InitializeComponent();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            SqlConnection con = new SqlConnection();
            con.ConnectionString = "Data Source="+textBoxServerName.Text+ ";Initial Catalog=FDRHH;User ID=FDM;Password=fdmhh";
            try
            {
                con.Open();
                string userid = textBoxUserName.Text;
                string password = textBoxPassword.Text;
                SqlCommand cmd = new SqlCommand("select username,password from permission where username='"
                    + textBoxUserName.Text + "'and password='" + textBoxPassword.Text + "'", con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    Hide();
                    FormMain formmain = new FormMain();
                    formmain.Show();
                }
                else
                {
                    MessageBox.Show("Invalid Login Please check Username and Password");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid Connection Please check ServerName");
            }
            con.Close();

        }
    }
}
