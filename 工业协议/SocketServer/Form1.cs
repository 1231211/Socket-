using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketServer
{
    public partial class Form1 : Form
    {
        Server server = null;
        public Form1()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }
        /// <summary>
        /// 开启监听
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "")
            {
                MessageBox.Show("ip地址和端口号不能为空!");
            }
            else
            {
                server = null;
                server = new Server(textBox1.Text, textBox2.Text);
                server.serverEvent += new serverDelegate(ShowMessage);
                server.comboxEvent += new comboxDelegate(ShowCombox);
                server.ServerStart();
            }
        }
        /// <summary>
        /// 显示后台日志
        /// </summary>
        /// <param name="msg"></param>
        public void ShowMessage(string msg)
        {
            richTextBox1.AppendText(msg);
            richTextBox1.HideSelection = false;
        }
        /// <summary>
        /// 添加客户端ip
        /// </summary>
        /// <param name="ipmsg"></param>
        public void ShowCombox(bool cutoradd, string ipmsg)
        {
            if(cutoradd)
            {
                comboBox1.Items.Add(ipmsg);
            }
            else
            {
                comboBox1.Items.Remove(ipmsg);
            }
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if(comboBox1.SelectedItem.ToString()!="")
            {
                server.PutMessage(comboBox1.SelectedItem.ToString(), richTextBox2.Text);
                richTextBox2.Text = "";
                ShowMessage(comboBox1.SelectedItem.ToString() + ":发送成功!\r\n");
            }
            else
            {
                MessageBox.Show("请选择要发送的客户端!");
            }
        }
    }
}
