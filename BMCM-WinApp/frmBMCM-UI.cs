using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Configuration;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace BMCM_WinApp
{
    public partial class FrmAttrMgmt : Form
    {
        OracleConnection con = null;
        string mongoStr;
        readonly DataSet dsOfferMgmt = new DataSet();
        string offerCmd;
        string selectedOfferID;
        string selectedOfferName;
        int selectedOfferIndex; 
        string selectedComponent;
        private bool TablesAlreadyAdded;
        string tblPrefix;
        
        public FrmAttrMgmt()
        {
            string envCon;
            if (cbEnv == null)
            { envCon = "DEV2"; }
            else
            { envCon = cbEnv.Text; }
            this.SetConnection(envCon);
            InitializeComponent();
        }
        public void CbEnvLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select lookup_id,lookup_value from ctl_lookup where lookup_type = 'ENVIRONMENT'";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbEnv.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbServiceLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select lookup_id,lookup_value from ctl_lookup where lookup_type = 'ATTRIBUTE_SERVICE' union select 0,'None' from dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbService.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbStateLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select distinct state from bm_submarket union select '.' from dual order by 1";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbState.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbCityLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select distinct city from bm_submarket where state ='"+cbState.Text+"' union select '.' from dual order by 1";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();   
            dt.Load(dr);
//            cbCity.DataSource = null;
            cbCity.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbWCLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select DISTINCT WIRE_CENTER from bm_submarket where state ='"+cbState.Text+"' and city = '"+cbCity.Text+"' union select '.' from dual order by 1";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
//            cbWC.DataSource = null;
            cbWC.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbChannelLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select lookup_id,lookup_source from ctl_lookup where lookup_type = 'CUSTOMER_CHANNEL' union select 0,null from dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbChannel.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbAttrLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select distinct a.attribute_id,a.attribute_name||decode(b.attribute_name,null,'*') attribute_name from ctl_attribute_master a left outer join  vw_Ctl_attr_management b on a.attribute_name=b.attribute_name union select 0,null from dual";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbAttr.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbBundleLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select distinct bundle_promo_id from "+tblPrefix+"product_offering";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbBundle.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbOfferTypeLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select distinct offer_type from " + tblPrefix + "product_offering";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbOfferType.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbOfferCatLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select distinct offer_category from " + tblPrefix + "product_offering";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbOfferCat.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbStatusLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select lookup_id,lookup_value  from ctl_lookup where UPPER(lookup_type) = 'OFFER_STATUS' union select 0,null from dual ";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbStatus.DataSource = dt.DefaultView;
            cbAttrStatus.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void CbBillingLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            cmd.CommandText = "select lookup_id,lookup_value  from ctl_lookup where UPPER(lookup_type) = 'OFFERBILLINGTYPE' union select 0,null from dual ";
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
            cbBillType.DataSource = dt.DefaultView;
            dr.Close();
        }
        public void DgAttrLoad()
        {
            string srvFilter;
            string channelFilter;
            string attrFilter;
            string AttrStatusFilter;
            OracleCommand cmd = con.CreateCommand();
             if (chkSrv.Checked)
            {
                srvFilter = null;
                cbService.SelectedIndex = 0;
            }
            else
            {
                srvFilter = " and service_name = '" + cbService.Text + "'";
            }
            if (chkChnl.Checked)
            {
                channelFilter = null;
                //cbChannel.SelectedIndex = 0;
            }
            else
            {
                channelFilter = " and channel = '" + cbChannel.Text + "'";
            }
            if (chkAttr.Checked)
            {
                attrFilter = null;
                //cbAttr.SelectedIndex = 0;
            }
            else
            {
                attrFilter = " and attribute_name = '" + cbAttr.Text + "'";
            }
            if (chkAttrStatus.Checked)
            {
                AttrStatusFilter = null;
            }
            else
            {
                AttrStatusFilter = " and status_id = '" + cbAttrStatus.SelectedValue + "'";
            }
            cmd.CommandText = "select assc_type, assc_id, Channel,Attribute_name, attribute_value, status_id from vw_ctl_attr_management " +
                "where 1=1 " + srvFilter + channelFilter + attrFilter+ AttrStatusFilter;
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dt = new DataTable();
            dt.Load(dr);
//            ds.Tables.Add(dt);
            dgAttr.DataSource = dt.DefaultView;
            dr.Close();

        }
        public void DgOfferLoad()
        {
            string dtRange ;
            string bundleFilter;
            string billingFilter;
            string statusFilter;
            string offerTypeFilter;
            string offerCatFilter;            
            OracleCommand cmd = con.CreateCommand();
            if (chkActive.Checked)
            {
                dtRange = " and sysdate between nvl(sale_eff_date, sysdate) and nvl(sale_exp_date,sysdate) and bdl_Sale_Exp_date is null ";
            }
            else
            {
                dtRange = null;
            }
            if (chkBundle.Checked)
            {
                bundleFilter = null;
                cbBundle.SelectedIndex = 0;
            }
            else
            {
                bundleFilter = " and bundle_promo_id = '" + cbBundle.Text + "'"; 
            }

            if (chkBilling.Checked)
            {
                billingFilter = null;
            }
            else
            {
                billingFilter = " and offer_billing_Type = '" + cbBillType.Text + "'";
            }
            if (chkStatus.Checked || cbSubEnv.Text!="CTL_")
            {
                statusFilter = null;
            }
            else
            {
                statusFilter = " and status_id = '" + cbStatus.SelectedValue + "'";
            }
            if (chkOfferType.Checked)
            {
                offerTypeFilter = null;
            }
            else
            {
                offerTypeFilter = " and offer_type = '" + cbOfferType.SelectedValue + "'";
            }
            if (chkOfferCat.Checked )
            {
                offerCatFilter = null;
            }
            else
            {
                offerCatFilter = " and offer_category = '" + cbOfferCat.SelectedValue + "'";
            }
            if (cbSubEnv.Text=="CTL_")
            { tblPrefix = cbSubEnv.Text; }
            else
            { tblPrefix = null; }
            cmd.CommandText = "select product_offer_id,offer_name,valid_From,bundle_promo_id,sale_eff_date,sale_exp_date,bdl_Sale_Exp_date from "+ tblPrefix + "product_offering " +
                "where upper(offer_name) like '%" + txtName.Text.ToUpper().ToString() + "%'"+ billingFilter + dtRange + statusFilter + bundleFilter+offerCatFilter+offerTypeFilter+
                " and product_offer_id like '%"+txtOfferID.Text+"%'" ;
            offerCmd = "select product_offer_id from " + tblPrefix + "product_offering " +
                "where upper(offer_name) like '%" + txtName.Text.ToUpper().ToString() + "%'" + billingFilter + dtRange + statusFilter + bundleFilter + offerCatFilter + offerTypeFilter +
                " and product_offer_id like '%" + txtOfferID.Text + "%'"; 
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            using (DataTable dtOffer = new DataTable())
            {
            }
            if (dsOfferMgmt.Tables.Contains("dtComp"))
            { dsOfferMgmt.Tables["dtComp"].Clear(); }

            if (dsOfferMgmt.Tables.Contains("dtOffer"))
            { dsOfferMgmt.Tables["dtOffer"].Clear(); }      
//            dsOfferMgmt.Clear();
            dsOfferMgmt.Load(dr, LoadOption.OverwriteChanges, "dtOffer");
            //            dsOfferMgmt.Tables.Add(dtOffer);
            //            dtOffer.Load(dr);
            dgvOffer.DataSource = dsOfferMgmt.Tables["dtOffer"];
   //         dgvOffer.DataSource = dtOffer.DefaultView;
            dr.Close();
            ComponentLoad();
        }
        public void ComponentLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            if (cbSubEnv.Text == "CTL_")
            { tblPrefix = cbSubEnv.Text; }
            else
            { tblPrefix = null; }

            cmd.CommandText = "select offer_component,product_offer_id from " + tblPrefix + "product_offer_defnit where product_offer_id in (" + offerCmd + ")";

            //            cmd.CommandText = "select product_offer_id,offer_name,valid_From,bundle_promo_id,sale_eff_date,sale_exp_date,bdl_Sale_Exp_date from " + tblPrefix + "product_offering " +
            //              "where upper(offer_name) like '%" + txtName.Text.ToUpper().ToString() + "%'" + billingFilter + dtRange + statusFilter + bundleFilter + offerCatFilter + offerTypeFilter;
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            using (DataTable dtComp = new DataTable())
            {
            }
            //dtComp.Load(dr);
            dsOfferMgmt.Relations.Clear();
            dsOfferMgmt.Load(dr, LoadOption.OverwriteChanges, "dtComp");
            dr.Close();

            DataRelation dtr = new DataRelation("OfferComp", dsOfferMgmt.Tables["dtOffer"].Columns["product_offer_id"], dsOfferMgmt.Tables["dtComp"].Columns["product_offer_id"]);
            dsOfferMgmt.Relations.Clear();
            dsOfferMgmt.Relations.Add(dtr);

        }
        public void DgvCompLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            if (cbSubEnv.Text == "CTL_")
            { tblPrefix = cbSubEnv.Text; }
            else
            { tblPrefix = null; }

            cmd.CommandText = "select offer_component,product_offer_id,' is '|| is_primary||' and '|| decode(is_mandatory,'NO','NOT MANDATORY',decode(is_mandatory,'YES','MANDATORY',is_mandatory)) CONDITION " +
                " from " + tblPrefix + "product_offer_defnit where product_offer_id = '" + selectedOfferID + "' ORDER BY 2";
             cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dtComp = new DataTable();
            dtComp.Load(dr);
            dgvComp.DataSource = dtComp;
        }
        public void DgvPriceLoad()
        {
            string priceDtRange;
            OracleCommand cmd = con.CreateCommand();
            if (cbSubEnv.Text == "CTL_")
            { tblPrefix = cbSubEnv.Text; }
            else
            { tblPrefix = null; }
            string sts;
            if (cbSubEnv.Text == "CTL_")
            { sts = " ,status_id ";  }
            else
            { sts = null; }
            if (chkActive.Checked)
            {
                priceDtRange = " and sales_expiration_date is null ";
            }
            else
            {
                priceDtRange = null;
            }
            cmd.CommandText = "    select distinct MRC,OTC,price_type,price_ind,sales_expiration_date"+sts+" from " + tblPrefix + "price_book_detail_summary where price_book_detail_id in (select distinct price_book_detail_id from " + tblPrefix +
                "price_book_detail where price_id like '%" + selectedOfferID + "~"+selectedComponent+"%') "+ priceDtRange;
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dtPrice = new DataTable();
            dtPrice.Load(dr);
            dgvPrice.DataSource = dtPrice;
        }
        public void DgvPPPLoad()
        {
            string priceDtRange;
            OracleCommand cmd = con.CreateCommand();
            if (cbSubEnv.Text == "CTL_")
            { tblPrefix = cbSubEnv.Text; }
            else
            { tblPrefix = null; }
            string sts;
            if (cbSubEnv.Text == "CTL_")
            { sts = " ,status_id "; }
            else
            { sts = null; }
            if (chkActive.Checked)
            {
                priceDtRange = " and sales_expiration_date is null ";
            }
            else
            {
                priceDtRange = null;
            }
            cmd.CommandText = "select  offer_name,comp_name,ppp_offer_id,company_division from ctl_ds_prod_ppp_stg where offer_name in (select offer_name from ctl_PRODUCT_OFFERING where product_offer_id ='"+selectedOfferID+"') and comp_name like '"+ selectedComponent+"%'";
                
//                "    select distinct MRC,OTC,price_type,price_ind,sales_expiration_date" + sts + " from " + tblPrefix + "price_book_detail_summary where price_book_detail_id in (select distinct price_book_detail_id from " + tblPrefix +
//                "price_book_detail where price_id like '%" + selectedOfferID + "~" + selectedComponent + "%') " + priceDtRange;
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            DataTable dtPPP = new DataTable();
            dtPPP.Load(dr);
            dgvPPP.DataSource = dtPPP;
        }
        public void PricingLoad()
        {
            OracleCommand cmd = con.CreateCommand();
            if (cbSubEnv.Text == "CTL_")
            { tblPrefix = cbSubEnv.Text; }
            else
            { tblPrefix = null; }
            cmd.CommandText = " select distinct MRC,OTC,price_type,price_ind,sales_expiration_date,status_id from ctl_price_book_detail_summary where price_book_detail_id in (" + selectedOfferID + ")";
            //            cmd.CommandText = "select product_offer_id,offer_name,valid_From,bundle_promo_id,sale_eff_date,sale_exp_date,bdl_Sale_Exp_date from " + tblPrefix + "product_offering " +
            //              "where upper(offer_name) like '%" + txtName.Text.ToUpper().ToString() + "%'" + billingFilter + dtRange + statusFilter + bundleFilter + offerCatFilter + offerTypeFilter;
            cmd.CommandType = CommandType.Text;
            OracleDataReader dr = cmd.ExecuteReader();
            _ = new DataTable();
            //dtComp.Load(dr);
            dsOfferMgmt.Relations.Clear();
            dsOfferMgmt.Load(dr, LoadOption.OverwriteChanges, "dtComp");
            dr.Close();

            DataRelation dtr = new DataRelation("OfferComp", dsOfferMgmt.Tables["dtOffer"].Columns["product_offer_id"], dsOfferMgmt.Tables["dtComp"].Columns["product_offer_id"]);
            dsOfferMgmt.Relations.Clear();
            dsOfferMgmt.Relations.Add(dtr);
        }
        public void DgvMongoLoad()
        {
            IMongoCollection<Pricing> pricing;
            mongoStr = ConfigurationManager.ConnectionStrings["BMCM_WinApp.Properties.Settings.Devsi4Mongo"].ConnectionString; ;
            MongoClient client = new MongoClient(mongoStr);
            IMongoDatabase db = client.GetDatabase("BMP_PRICING_2");
            
            pricing = db.GetCollection<Pricing>("productPriceBook");
            //pricing.Find(Pricing => true).ToList();
           // List<Pricing> list = pricing.AsQueryable().ToList<Pricing>();
            //dgvMongoPricing.DataSource = list;
            //IMongoCollection<Pricing> Collection = db.GetCollection<Pricing>("Pricing"); if (cbSubEnv.Text == "CTL_")
        }
        public void DgvMongoAttrLoad()
        {
            IMongoCollection<AttrMongo> attrMongo;
            mongoStr = ConfigurationManager.ConnectionStrings["BMCM_WinApp.Properties.Settings.Devsi4Mongo"].ConnectionString; ;
            MongoClient client = new MongoClient(mongoStr);
            IMongoDatabase db = client.GetDatabase("BMP_ATTRIBUTE_MGMT_2");

            attrMongo = db.GetCollection<AttrMongo>("attributeDetail");
            List<AttrMongo> list = attrMongo.AsQueryable().ToList<AttrMongo>();
            dgvMongoAttr.DataSource = list;
            //pricing.Find(Pricing => true).ToList();
            // List<Pricing> list = pricing.AsQueryable().ToList<Pricing>();
            //dgvMongoPricing.DataSource = list;
            //IMongoCollection<Pricing> Collection = db.GetCollection<Pricing>("Pricing"); if (cbSubEnv.Text == "CTL_")
        }
        public void SetConnection(string envCon)
        {
            if (envCon=="")
            { envCon = "DEV2"; }
            string conStr;
            conStr = ConfigurationManager.ConnectionStrings["BMCM_WinApp.Properties.Settings."+ envCon].ConnectionString;
            con = new OracleConnection(conStr);
            try { 
                con.Open();
            }
            catch { }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            SetConnection(cbEnv.Text);
            this.CbServiceLoad();
            this.CbChannelLoad();
            if (chkRefresh.Checked)
            {
                this.CbAttrLoad();
            }
            this.CbEnvLoad();
            this.CbBundleLoad();
            this.CbBillingLoad();
            this.CbStatusLoad();
            this.CbOfferTypeLoad();
            this.CbOfferCatLoad();
            this.CbStateLoad();
            this.CbCityLoad();
            this.CbWCLoad();
            this.dgOfferComp.DataSource = dsOfferMgmt.Tables[0];
            if (!TablesAlreadyAdded)
            { AddCustomDataTableStyle(); }
            DgvMongoLoad();
            // con.Close();
        }
        private void CbService_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkRefresh.Checked)
                this.DgAttrLoad();
        }

        private void CbChannel_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DgAttrLoad();
        }

        private void DgAttr_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            foreach (DataGridViewRow row in dgAttr.Rows)
            {
                int status = Convert.ToInt32(row.Cells["status_id"].Value);
                if (status == 1)
                {
                    row.DefaultCellStyle.BackColor = Color.Red;
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
                if (status == 3)
                {
                    row.DefaultCellStyle.BackColor = Color.Green;
                    row.DefaultCellStyle.ForeColor = Color.White;
                }
            }
        }

        private void CbAttr_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DgAttrLoad();
        }

        private void FrmAttrMgmt_FormClosed(object sender, FormClosedEventArgs e)
        {
            con.Close();
        }

        private void CbEnv_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.SetConnection(cbEnv.Text);
            //InitializeComponent();
            if (chkRefresh.Checked)
                this.DgAttrLoad();
            this.CbServiceLoad();
            this.CbChannelLoad();
            if (chkRefresh.Checked)
                this.CbAttrLoad();
            this.CbBundleLoad();
            this.CbBillingLoad();
            this.CbStatusLoad();
        }
                
        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }

        private void CbBillType_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }

        private void ChkActive_CheckedChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
            this.DgvPriceLoad();
        }

        private void CbStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }

        private void CbBundle_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }

        private void ChkBundle_CheckedChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }

        private void ChkStatus_CheckedChanged(object sender, EventArgs e)
        {
            this.cbStatus.SelectedIndex = 0;
            this.DgOfferLoad();
        }

        private void ChkBilling_CheckedChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }

        private void ChkSrv_CheckedChanged(object sender, EventArgs e)
        {
            this.DgAttrLoad();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.DgAttrLoad();
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            this.DgAttrLoad();
        }

        private void ChkAttrStatus_CheckedChanged(object sender, EventArgs e)
        {
            this.DgAttrLoad();
        }

        private void CbSubEnv_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.SetConnection(cbEnv.Text);
            //InitializeComponent();
            if (chkRefresh.Checked)
            {
                this.CbAttrLoad();
                this.DgvMongoAttrLoad();
            }
            this.DgOfferLoad();
            this.CbServiceLoad();
            this.CbChannelLoad();
            if (chkRefresh.Checked)
            {
                this.CbAttrLoad();
                this.DgvMongoAttrLoad();
            }
            this.CbBundleLoad();
            this.CbBillingLoad();
            this.CbStatusLoad();
        }

        private void CbOfferType_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }
        private void CbOfferCat_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }
        private void ChkOfferType_CheckedChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }
        private void ChkOfferCat_CheckedChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }
        private void AddCustomDataTableStyle()
        {
            DataGridTableStyle ts1 = new DataGridTableStyle
            {
                MappingName = "dtOffer",
                // Set other properties.
                AlternatingBackColor = Color.LightGray
            };

            DataGridColumnStyle txtOffID = new DataGridTextBoxColumn
            {
                MappingName = "product_offer_id",
                HeaderText = "Offer ID",
                Width = 100
            };
            ts1.GridColumnStyles.Add(txtOffID);

            DataGridColumnStyle txtColOffer = new DataGridTextBoxColumn
            {
                MappingName = "OFFER_NAME",
                HeaderText = "Offer Name",
                Width = 250
            };
            ts1.GridColumnStyles.Add(txtColOffer);

            DataGridColumnStyle txtCol2 = new DataGridTextBoxColumn
            {
                MappingName = "bundle_Promo_id",
                HeaderText = "Bundle Code",
                Width = 100
            };
            ts1.GridColumnStyles.Add(txtCol2);


            DataGridTableStyle ts2 = new DataGridTableStyle
            {
                MappingName = "dtComp",
                // Set other properties.
                AlternatingBackColor = Color.LightBlue
            };

            // Create the second table style with columns.
            DataGridColumnStyle txtOffID2 = new DataGridTextBoxColumn
            {
                MappingName = "product_offer_id",
                HeaderText = "Offer ID",
                Width = 100
            };
            ts2.GridColumnStyles.Add(txtOffID2);

            DataGridColumnStyle txtOffComp = new DataGridTextBoxColumn
            {
                MappingName = "offer_component",
                HeaderText = "Offer Component",
                Width = 500
            };
            ts2.GridColumnStyles.Add(txtOffComp);


            /* Add the DataGridTableStyle instances to 
            the GridTableStylesCollection. */
            dgOfferComp.TableStyles.Add(ts1);
            dgOfferComp.TableStyles.Add(ts2);


            // Sets the TablesAlreadyAdded to true so this doesn't happen again.
            TablesAlreadyAdded = true;
        }
        private void DgOfferComp_Click(object sender, EventArgs e)
        {
            if (dgOfferComp.CurrentCell.ColumnNumber == 0)
            {
                MessageBox.Show("Offer Id : = " + dgOfferComp[dgOfferComp.CurrentCell] + " is selected");
            }
        }
        private void DgOfferComp_CurrentCellChanged(object sender, EventArgs e)
        {
            MessageBox.Show("Offer Id : = " + dgOfferComp[dgOfferComp.CurrentCell] + " is clicked");
        }
        private void DgOfferComp_MouseDown(object sender, MouseEventArgs e)
        {
            DataGrid myGrid = (DataGrid)sender;
            System.Windows.Forms.DataGrid.HitTestInfo hti;
            hti = myGrid.HitTest(e.X, e.Y);
            string message = "You clicked ";

            switch (hti.Type)
            {
                case System.Windows.Forms.DataGrid.HitTestType.None:
                    message += "the background.";
                    break;
                case System.Windows.Forms.DataGrid.HitTestType.Cell:
                    message += "cell at row " + hti.Row + ", col " + hti.Column;
                    break;
                case System.Windows.Forms.DataGrid.HitTestType.ColumnHeader:
                    message += "the column header for column " + hti.Column;
                    break;
                case System.Windows.Forms.DataGrid.HitTestType.RowHeader:
                    message += "the row header for row " + hti.Row;
                    break;
                case System.Windows.Forms.DataGrid.HitTestType.ColumnResize:
                    message += "the column resizer for column " + hti.Column;
                    break;
                case System.Windows.Forms.DataGrid.HitTestType.RowResize:
                    message += "the row resizer for row " + hti.Row;
                    break;
                case System.Windows.Forms.DataGrid.HitTestType.Caption:
                    message += "the caption";
                    break;
                case System.Windows.Forms.DataGrid.HitTestType.ParentRows:
                    message += "the parent row";
                    break;
            }

            MessageBox.Show(message);
        }
        private void DgvOffer_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void DgvOffer_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            dgvOffer.CurrentRow.Selected = true;
            selectedOfferIndex = dgvOffer.CurrentRow.Index;
            selectedOfferName = dgvOffer.Rows[e.RowIndex].Cells["Offer_Name"].FormattedValue.ToString();
            selectedOfferID = dgvOffer.Rows[e.RowIndex].Cells["Product_OffeR_ID"].FormattedValue.ToString();
            if (dgvOffer.CurrentCell.ColumnIndex.ToString()=="0")
            {
                DgvCompLoad();
                txtSelOffer.Text = selectedOfferID;
            }    
        }
        private void DgvComp_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            dgvComp.CurrentRow.Selected = true;
            if (dgvComp.CurrentCell.ColumnIndex.ToString() == "0")
            {
                selectedComponent = dgvComp.Rows[e.RowIndex].Cells["Component_name"].FormattedValue.ToString();
                txtSelComp.Text = selectedComponent;
                DgvPPPLoad();
                dgvOffer.Rows[selectedOfferIndex].Selected=true;
                if (chkPrice.Checked)
                {
                    DgvPriceLoad();
                }
            }
        }
        private void DgOfferComp_Navigate(object sender, NavigateEventArgs ne)
        {

        }
        private void CbState_SelectedIndexChanged(object sender, EventArgs e)
        {
            CbCityLoad();
        }
        private void CbCity_SelectedIndexChanged(object sender, EventArgs e)
        {
            CbWCLoad();
        }
        private void chkRefresh_CheckedChanged(object sender, EventArgs e)
        {
            DgvMongoAttrLoad();
        }
        private void txtOfferID_TextChanged(object sender, EventArgs e)
        {
            this.DgOfferLoad();
        }

        private void pgOffer_Click(object sender, EventArgs e)
        {

        }
    }

    internal class Pricing
    {
        [BsonId]
        public object Id { get; set; }
        [BsonElement("isDiscountApplicable")]
        public object isDiscountApplicable { get; set; }
        [BsonElement("prices")]
        public object prices { get; set; }
        [BsonElement("priceableProductAttributes")]
        public object priceableProductAttributes { get; set; }
        [BsonElement("productName")]
        public object productName { get; set; }
        [BsonElement("recurTermFee")]
        public object recurTermFee { get; set; }
        [BsonElement("discounts")]
        public object discounts { get; set; }
        [BsonElement("_class")]
        public object _class { get; set; }

    }
    internal class AttrMongo
    {
        [BsonId]
        public object Id { get; set; }
        [BsonElement("serviceName")]
        public object serviceName { get; set; }
        [BsonElement("level")]
        public object level { get; set; }
        [BsonElement("associationType")]
        public object associationType { get; set; }
        [BsonElement("associationId")]
        public object associationId { get; set; }
        [BsonElement("salesChannel")]
        public object salesChannel { get; set; }
        [BsonElement("billingType")]
        public object billingType { get; set; }
        [BsonElement("customerType")]
        public object customerType { get; set; }
        [BsonElement("city")]
        public object city { get; set; }
        [BsonElement("state")]
        public object state { get; set; }
        [BsonElement("wireCenter")]
        public object wireCenter { get; set; }
        [BsonElement("attributes")]
        public object attributes { get; set; }
        [BsonElement("attributeKey")]
        public object attributeKey { get; set; }
        [BsonElement("attributeValue")]
        public object attributeValue { get; set; }
        [BsonElement("_class")]
        public object _class { get; set; }
    }
 }