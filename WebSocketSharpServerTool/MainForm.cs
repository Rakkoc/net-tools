﻿using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using WebSocketSharp.Server;

namespace WebSocketSharpServerTool
{
    public partial class MainForm : Form
    {
        WebSocketServer server;
        static int counter = 0;

        public MainForm()
        {
            InitializeComponent();
            ServiceBehavior.Open += ServiceBehavior_Open;
        }

        private void ServiceBehavior_Open(ServiceBehavior client)
        {
            Invoke((MethodInvoker)delegate
            {
                ClientForm form = new ClientForm(client);
                Interlocked.Increment(ref counter);
                form.Text = "Client " + counter;
                form.Owner = this;
                form.Show(this);
            });
        }

        private void CreateServer(Uri uri, string thumbprint = null, 
            string pfxPath = null, string pfxPassword = null)
        {
            server = new WebSocketServer(uri.GetLeftPart(System.UriPartial.Authority));

            X509Certificate2 cert = null;
            if (!string.IsNullOrEmpty(pfxPath))
            {
                cert = new X509Certificate2(pfxPath, pfxPassword);
            }
            else if (!string.IsNullOrEmpty(thumbprint))
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certificates =
                    store.Certificates.Find(X509FindType.FindByThumbprint,
                        thumbprint, false);
                if (certificates.Count > 0)
                {
                    cert = certificates[0];
                };
                store.Close();
            }

            if (uri.Scheme.Equals("wss") && cert == null)
            {
                MessageBox.Show(this, "Certificate not found.", this.Text);
                server = null;
                return;
            }

            server.SslConfiguration.ServerCertificate = cert;

            server.AddWebSocketService<ServiceBehavior>(uri.AbsolutePath);

            server.Start();
        }

        private void start_Click(object sender, System.EventArgs e)
        {
            if (server != null) return;

            try
            {
                if (pfxFileOption.Checked)
                {
                    CreateServer(new Uri(url.Text), pfxPath: this.pfxPath.Text, 
                        pfxPassword: this.pfxPassword.Text);
                }
                else
                {
                    CreateServer(new Uri(url.Text), this.thumbprint.Text);
                }

                if (server == null)
                    return;

                LockControls(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, this.Text);
                if (server != null)
                {
                    server.Stop();
                    server = null;
                }
            }
        }

        private void stop_Click(object sender, EventArgs e)
        {
            if (server == null) return;

            server.Stop();
            server = null;
            LockControls(false);
        }

        private void browseForPfx_Click(object sender, EventArgs e)
        {
            DialogResult r = openFileDialog.ShowDialog();
            if (r == DialogResult.OK)
            {
                pfxPath.Text = openFileDialog.FileName;
            }
        }

        private void pfxFileOption_CheckedChanged(object sender, EventArgs e)
        {
            EnablePfxControls(pfxFileOption.Checked);
        }

        private void EnablePfxControls(bool enabled)
        {
            pfxPath.Enabled = enabled;
            browseForPfx.Enabled = enabled;
            pfxPassword.Enabled = enabled;
            thumbprint.Enabled = !enabled;
        }

        private void LockControls(bool locked)
        {
            start.Enabled = !locked;
            stop.Enabled = locked;
            thumbprint.ReadOnly = locked;
            url.ReadOnly = locked;
            browseForPfx.Enabled = !locked;
            pfxPassword.ReadOnly = locked;
            privateKeyOption.Enabled = !locked;
        }
    }
}
