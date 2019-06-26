using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Threading;
using Baidu;
using Newtonsoft;
using Newtonsoft.Json.Linq;

namespace 百度ai图片上传工具
{
    public partial class Form1 : Form
    {

          string API_KEY = "dpfx3TvXmow5TriezSOlfu6p";
         string SECRET_KEY = "EHhPCRDTV7GQo3gaPZM5zI1ZRB6jnuP5";

        //string API_KEY = "N4fNwt5LzCNa1hX88nPI93hZ";
        //string SECRET_KEY = "VKxr1FROhueYGesXk0pomr2YVRu0jyZG";

        private static FileStream F = new FileStream(@"D:\error.txt",
             FileMode.OpenOrCreate, FileAccess.ReadWrite);
        StreamWriter sw = new StreamWriter(F);
        delegate void setList(string str);


        
        public Form1()
        {
            InitializeComponent();
        }

        public void Settext(string str)
        {
            this.listBox1.Items.Add(str);
        }
        public void Settext2(string str)
        {
            this.listBox2.Items.Add(str);
        }
        FileInfo[] files;
        private void button1_Click(object sender, EventArgs e)
        {
            //打开选择路径
            FolderBrowserDialog path = new FolderBrowserDialog();
            path.ShowDialog();
            this.textBox1.Text = path.SelectedPath;//获取到文件夹路径

            //获取所有的文件
            if (this.textBox1.Text == "")
            {
                return;
            }
          

            DirectoryInfo root = new DirectoryInfo(this.textBox1.Text);

            files = root.GetFiles();

            for (int i = 0; i < files.Count(); i++)
            {
                this.listBox1.Items.Add(files[i].Name);


            }
            this.listBox1.Items.Add("获取到" + files.Count() + "个文件");

         
        }

       






        private void button2_Click(object sender, EventArgs e)
        {

            sw.WriteLine(DateTime.Now + "开始上传");

              var task = Task.Run(() =>
            {

                for (int i = 0; i < files.Count(); i++)
                {
                    //this.listBox1.Items.Add(files[i].Name + "开始上传");
                    //先获取图片是否有对应关系
                    string sql = "SELECT a.fGoodsCode,a.fGoodsName,b.fClrName,a.fStdSellUp,a.fSizeDesc,d.fMKName FROM t_BOMM_GoodsMst a  inner join t_COPM_ClrMst b on a.fClrCode=b.fClrCode inner join t_BMSM_Maker d on a.fMkCode = d.fMKCode where fsimplepicfile=@id and a.fDevProperty<>'2'";

                    SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@id", files[i].Name)
            };
                    DataTable o = SqlHelper.SqlHelper.ExcuteDataTable(sql, parameters);

                    if (o.Rows.Count >= 1)
                    {

                    foreach (DataRow item in o.Rows)
                    {


                        Run(files[i].FullName, item["fGoodsCode"].ToString());
                            
                            System.Threading.Thread.Sleep(1000);

                        }

                    }



                    


                }


                sw.Close();
                F.Close();
                MessageBox.Show("全部上传完成");




            });


          

        }


        private void Run(string FullName, string fGoodsCode)
        {

           
               
                
                    //var task = Task.Run(() =>
                    //{
                     
                        try
                        {
                          
                            var image = File.ReadAllBytes(FullName);
                            // 调用商品检索—入库, 图片参数为本地图片，可能会抛出网络等异常，请使用try/catch捕获
                            var client = new Baidu.Aip.ImageSearch.ImageSearch(API_KEY, SECRET_KEY);
                            //var result = client.ProductAdd(image);
                          
                            // 如果有可选参数
                            var options = new Dictionary<string, object>{
                        {"brief", "{\"fGoodsCode\":\""+fGoodsCode+"\"}"},
                         {"url",FullName }
                               

                            };
                            // 带参数调用商品检索—入库, 图片参数为本地图片
                            // \"fGoodsName\":\""+item["fGoodsName"].ToString()+"\",\"fClrName\":\""+item["fClrName"].ToString()+"\",\"fStdSellUp\":\""+item["fStdSellUp"].ToString()+"\",\"fSizeDesc\":\""+item["fSizeDesc"].ToString()+"\",\"fMKName\":\""+item["fMKName"].ToString()+"\",\"url\":\""+files[i].Name+"\"}"
                           var result = client.ProductAdd(image, options);

                        if(result.Count == 2)
                            {

                                string id = Guid.NewGuid().ToString();
                                string sql = "insert into BaiduUpload_info values(@fGoodsCode,@cont_sign,@fileName,@id,@upLoadDate)";
                                SqlParameter[] parameters = new SqlParameter[] {
                                 new SqlParameter("@fGoodsCode",fGoodsCode),
                                 new SqlParameter("@cont_sign",result["cont_sign"].ToString()),
                                new SqlParameter("@fileName",FullName),
                                new SqlParameter("@id",id),
                                 new SqlParameter("@upLoadDate",DateTime.Now),
                                      };

                               int count= SqlHelper.SqlHelper.ExcuteNonQuery(sql,parameters);
                                if (count > 0)
                                {
                                 setList fc = new setList(Settext);
                                 this.BeginInvoke(fc, FullName + "上传成功");//调用代理
                                     Console.WriteLine(FullName+"上传成功");
                                }

                }
                else
                {

                    string error = result["error_code"].ToString();
                    string id = Guid.NewGuid().ToString();
                    string sql_text = "insert into BaiduUpload_err values(@id,@fGoodSCode,@flieName,@cont_same,@upLoadDate,@err_Code)";

                    SqlParameter[] parameters = new SqlParameter[] {
                                 new SqlParameter("@id",id),
                                 new SqlParameter("@fGoodSCode",fGoodsCode),
                                new SqlParameter("@flieName",FullName),
                                 new SqlParameter("@cont_same",result["cont_sign"].ToString()),

                                 new SqlParameter("@upLoadDate",DateTime.Now),
                                 new SqlParameter("@err_Code",error),
                                      };
                    int count = SqlHelper.SqlHelper.ExcuteNonQuery(sql_text, parameters);
                    if (error.Equals("216681"))
                    {
                       

                        if (count > 0)
                        {
                            setList f = new setList(Settext2);
                            this.BeginInvoke(f, FullName + "重复已记录");//调用代理
                        }




                    }
                    else
                    {
  
                    setList fc = new setList(Settext2);
                        this.BeginInvoke(fc, FullName + "状态码：" + error);//调用代理
                    }
                    
                    
                    
                    

                }

            


                           



                        }
                        catch (Exception ex)
                        {

                sw.WriteLine(fGoodsCode + "找不到：" + FullName);
                            Console.WriteLine(ex);
                            Console.WriteLine(FullName + "上传失败");
                        }




            //});

           


        }

        private void button3_Click(object sender, EventArgs e)
        {
            var task = Task.Run(() =>
             {
                 string sql = "select * from  BaiduUpload_info";
                DataTable dt= SqlHelper.SqlHelper.ExcuteDataTable(sql);
                 foreach(DataRow item in dt.Rows)
                 {
                    
                         try
                         {


                             var contSign = item["cont_sign"].ToString();

                             var client = new Baidu.Aip.ImageSearch.ImageSearch(API_KEY, SECRET_KEY);
                             var result = client.ProductDeleteBySign(contSign);
                             Console.WriteLine(result);
                         setList fc = new setList(Settext);
                         string sql2 = "delete  from  BaiduUpload_info where cont_sign=@cont_sign";
                         SqlParameter[] parameters2 = new SqlParameter[] {
                             new SqlParameter("@cont_sign",contSign)
                                      };


                         SqlHelper.SqlHelper.ExcuteNonQuery(sql2, parameters2);

                         this.BeginInvoke(fc, contSign+"已删除");//调用代理


                     }
                     catch(Exception EX)
                         {

                       

                         Console.WriteLine(EX);
                             Console.WriteLine("--------删除失败----------");
                         }


                    System.Threading.Thread.Sleep(500);




                 }
                 MessageBox.Show("全部删除");



             });


        }

        private void button4_Click(object sender, EventArgs e)
        {
            //找到奥特莱斯所有产品
            var task = Task.Run(() =>
            {

                if (this.textBox1.Text == "")
                {
                    MessageBox.Show("还没选择路径");
                    return;
                }



               


                    string sql = "SELECT fGoodsCode,fSimplePicFile FROM t_BOMM_GoodsMst where fSimplePicFile is not null and fCFlag='1' and fBrandCode='11' and fSimplePicFile<>'' and fDevProperty<>'2'";
                    SqlParameter[] parameters = new SqlParameter[] {

                                      };

                    DataTable dt = SqlHelper.SqlHelper.ExcuteDataTable(sql, parameters);

                    var client = new Baidu.Aip.ImageSearch.ImageSearch(API_KEY, SECRET_KEY);
              


                    foreach (DataRow item in dt.Rows)
                    {
                    try
                    {
                        var options = new Dictionary<string, object>{
                        {"brief", "{\"fGoodsCode\":\""+item["fGoodsCode"].ToString()+"\"}"},
                         {"url",Guid.NewGuid().ToString()}
                     };



                        var image = File.ReadAllBytes(this.textBox1.Text + "\\" + item["fSimplePicFile"].ToString());

                        var result = client.ProductAdd(image, options);


                        if (result.Count == 2)
                        {

                            string sql2 = "insert into BaiduUpload_info values(@fGoodsCode,@cont_sign)";
                            SqlParameter[] parameters2 = new SqlParameter[] {
                            new SqlParameter("@fGoodsCode",item["fGoodsCode"].ToString()),
                             new SqlParameter("@cont_sign",result["cont_sign"].ToString())
                                      };

                            int count = SqlHelper.SqlHelper.ExcuteNonQuery(sql2, parameters2);
                            if (count > 0)
                            {
                                setList fc = new setList(Settext);
                                this.BeginInvoke(fc, item["fGoodsCode"].ToString() + "上传成功");//调用代理
                                                                                             // Console.WriteLine(FullName + "上传成功");

                            }

                        }
                        else
                        {
                            setList fc = new setList(Settext2);

                            this.BeginInvoke(fc, item["fGoodsCode"].ToString() + "状态码：" + result["error_code"].ToString());//调用代理


                            Console.WriteLine(result);

                            sw.WriteLine(item["fGoodsCode"].ToString() + "状态码：" + result["error_code"].ToString());

                        }

                    }
                    catch (Exception ex)
                    {
                        setList fc = new setList(Settext2);
                        this.BeginInvoke(fc, item["fGoodsCode"].ToString() + "找不到文件");//调用代理


                    }

                    System.Threading.Thread.Sleep(500);

                }

                sw.Close();
                F.Close();


                MessageBox.Show("全部上传完成");


            });


        }
    }


}
