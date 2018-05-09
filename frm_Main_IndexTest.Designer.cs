namespace TOEC_Index_Test
{
    partial class frm_Main_IndexTest
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_Main_IndexTest));
            this.ni = new System.Windows.Forms.NotifyIcon(this.components);
            this.cms = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsm_Close = new System.Windows.Forms.ToolStripMenuItem();
            this.tms_AutoStart = new System.Windows.Forms.ToolStripMenuItem();
            this.cms.SuspendLayout();
            this.SuspendLayout();
            // 
            // ni
            // 
            this.ni.ContextMenuStrip = this.cms;
            this.ni.Icon = ((System.Drawing.Icon)(resources.GetObject("ni.Icon")));
            this.ni.Visible = true;
            // 
            // cms
            // 
            this.cms.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsm_Close,
            this.tms_AutoStart});
            this.cms.Name = "cms";
            this.cms.Size = new System.Drawing.Size(153, 70);
            this.cms.Opening += new System.ComponentModel.CancelEventHandler(this.cms_Opening);
            // 
            // tsm_Close
            // 
            this.tsm_Close.Name = "tsm_Close";
            this.tsm_Close.Size = new System.Drawing.Size(152, 22);
            this.tsm_Close.Text = "关闭";
            this.tsm_Close.Click += new System.EventHandler(this.tsm_Close_Click);
            // 
            // tms_AutoStart
            // 
            this.tms_AutoStart.Checked = true;
            this.tms_AutoStart.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tms_AutoStart.Name = "tms_AutoStart";
            this.tms_AutoStart.Size = new System.Drawing.Size(152, 22);
            this.tms_AutoStart.Text = "开机自启";
            this.tms_AutoStart.Click += new System.EventHandler(this.tms_AutoStart_Click);
            // 
            // frm_Main_IndexTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(290, 109);
            this.Name = "frm_Main_IndexTest";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.Load += new System.EventHandler(this.frm_Main_IndexTest_Load);
            this.cms.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon ni;
        private System.Windows.Forms.ContextMenuStrip cms;
        private System.Windows.Forms.ToolStripMenuItem tsm_Close;
        private System.Windows.Forms.ToolStripMenuItem tms_AutoStart;
    }
}

