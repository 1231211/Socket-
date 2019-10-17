using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SocketClient
{
    public partial class Form1 : Form
    {
        Client client = null;
        public Form1()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }
        //连接到服务端
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "")
            {
                MessageBox.Show("ip地址和端口号不能为空!");
            }
            else
            {
                client = null;
                client = new Client(textBox2.Text, textBox1.Text);
                client.clientEvent += new clientDelegate(ShowMessage);
                client.ClientStart();
            }
        }
        /// <summary>
        /// 消息显示
        /// </summary>
        /// <param name="msg"></param>
        public void ShowMessage(string msg)
        {
            richTextBox1.Text += msg;
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            client.PutServerMessage(richTextBox2.Text);
            richTextBox2.Text = "";
            ShowMessage("发送成功!\r\n");
        }
    }
}
