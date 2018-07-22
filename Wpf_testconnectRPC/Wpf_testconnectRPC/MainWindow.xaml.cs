using JsonRpc.CoreCLR.Client;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wpf_testconnectRPC
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private static object mainLockFileObj = "mainlockFile";
        private static object testLockFileObj = "testlockFile";
        private static object privateLockFileObj = "privatelockFile";
        static bool _DEBUG_FLAG = false;
        static string NeoCliJsonRPCUrl =   "http://112.35.60.124:10332"; //
        static string NeoHTTPTestUrl   =   "https://api.nel.group/api/testnet"; //
        static string NeoHTTPMainUrl   =   "https://api.nel.group/api/mainnet"; //

        private string privateFileName = @"d:\my_connect\private_block_index.txt";
        private string mainFileName    = @"d:\my_connect\main_block_index.txt";
        private string testFileName    = @"d:\my_connect\test_block_index.txt";

        private string mainDataDirection    = @"d:\my_connect\main_block\";
        private string testDataDirection    = @"d:\my_connect\test_block\";
        private string privateDataDirection = @"d:\my_connect\private_block\";

        private long main_Index = 0;
        private long test_Index = 0;
        private long private_Index = 0;

        enum BLOCK_Flag {
            main=1,
            test =2,
            priv =3
        };

        private class requestNewData {
            public bool newRequest;
            public int  request_Block_Flag;
            public long  block_Index;
        }

        //public string resultstr;

        private requestNewData reqNew = new requestNewData();

        //交易块缓冲区
        //public StringBuilder all_tx_block = new StringBuilder();

        //private HashSet<string> AccountRecord = new HashSet< string>();
        
        private Dictionary<string, double> AccountNEOs = new Dictionary<string, double>();
        private Dictionary<string, double> AccountGASs = new Dictionary<string, double>();
        public MainWindow()
        {
            this.Loaded += MainWindow_Loaded;
            this.Unloaded += MainWindow_Unloaded;
            this.Closed += MainWindow_Closed;
            this.Closing += MainWindow_Closing;

            InitializeComponent();
            Console.WriteLine("MainWindow!");
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("win_load!");
            if(!Directory.Exists(@"d:\my_connect")){
                Directory.CreateDirectory(@"d:\my_connect");       
            }

            GetMainBlockTask((int)BLOCK_Flag.main);
            GetMainBlockTask((int)BLOCK_Flag.test);
            GetMainBlockTask((int)BLOCK_Flag.priv);
        }
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("win_Unload!");
        }
        private void MainWindow_Closing(object sender, EventArgs e)
        {
            Console.WriteLine("MainWindow_Closing!");
            Run_flag = false;
            Thread.Sleep(500);
        }
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("MainWindow_Closed!");
        }
        private bool Run_flag = true ;

        //获得存在交易记录的主块的Index ID ，TRUE，表示使用测试链 FALSE 使用公有链
        private void GetMainBlockTask(int flag)
        {
            string logfilename = "";
            if(flag == (int)BLOCK_Flag.main) logfilename = mainFileName;
            if(flag == (int)BLOCK_Flag.test) logfilename = testFileName;
            if(flag == (int)BLOCK_Flag.priv) logfilename = privateFileName;
            
            Task taskGetMain = new Task(async()=>{
                string lastStr = ReadTxtLastLine(logfilename);
                if(lastStr.IndexOf("#")== 0)
                {
                    lastStr = lastStr.Substring(1);
                }
                long index =long.Parse(lastStr)+1;
                long blockCount = 0;
                int blockJS = 0;
                //index = 99999999999999;
                while (Run_flag)
                {
                    //DateTime start = DateTime.Now;
                    // Console.WriteLine(start.ToLongTimeString());
                    /*
                    WriteStringToFile(start.ToLongTimeString(), "d:\\logs.txt");
                    Thread.Sleep(1000);*/
                    //Console.WriteLine(index);
                    int find = 0;
                    find =await  FindINorOUTdata(index, flag);  //TRUE，表示使用测试链 FALSE 使用公有链

                    if (find == -1)
                    {
                        while (Run_flag)
                        {
                            find++;
                            if (find <= 100)
                                Thread.Sleep(100); //在 NEO 的 DBFT 共识机制下，每 15~20 秒生成一个区块
                            else
                                break;
                        }

                        blockJS = 0;
                    }
                    else
                    {
                        if (find == 1)
                        {
                            WriteStringToFileLOCK(index.ToString(), logfilename, true, flag);

                        }
                        if (blockJS % 100 == 0)
                        { //每100次统计一次 blockcount ======
                            blockJS = 0;
                            blockCount = await FindBlockCount(flag);
                            Console.WriteLine("Block Count:" + blockCount);
                        }
                        blockJS++;
                        if (flag == (int)BLOCK_Flag.test)
                        {
                            test_Index = index;
                            Console.WriteLine("test: " + index);
                            this.Dispatcher.Invoke(() =>
                            {
                                this.TestIndex.Content = index + " / " + blockCount;
                            });
                        }
                        if (flag == (int)BLOCK_Flag.main)
                        {
                            main_Index = index;
                            Console.WriteLine("main: " + index);
                            this.Dispatcher.Invoke(() =>
                            {
                                this.MainIndex.Content = index + " / " + blockCount;
                            });
                        }
                        if (flag == (int)BLOCK_Flag.priv)
                        {
                            private_Index = index;
                            Console.WriteLine("private: " + index);
                            this.Dispatcher.Invoke(() =>
                            {
                                this.PrivateIndex.Content = index + " / " + blockCount;
                            });
                        }

                        index++;
                    }

                    //if (flag == (int)BLOCK_Flag.main) main_Index = index;
                    //if (flag == (int)BLOCK_Flag.test) test_Index = index;
                    //if (flag == (int)BLOCK_Flag.priv) private_Index = index;




                }
                WriteStringToFileLOCK("#"+index, logfilename,true,flag);
                if(flag == (int)BLOCK_Flag.main)
                    Console.WriteLine("GetMainBlockTask exit!!!!!!!!!!!!!");
                if(flag == (int)BLOCK_Flag.test)
                    Console.WriteLine("GetTestBlockTask exit!!!!!!!!!!!!!");
                if (flag == (int)BLOCK_Flag.priv)
                    Console.WriteLine("GetPrivateBlockTask exit!!!!!!!!!!!!!");
            });
            taskGetMain.Start();
        }

        //定义回调
        private delegate void setTextValueCallBack(string value);
        private delegate void setSliderValueCallBack(long value);
        //private delegate void setTextAppendCallBack(string value);
        private delegate void setButtonValueCallBack(bool value);
        private delegate void setAccountValueCallBack(string value);

        //声明回调
        private setTextValueCallBack setCallBack;
        private setSliderValueCallBack setSpliderBack;
        //private setTextAppendCallBack setAppendBack;
        private setButtonValueCallBack setButtonBack;
        private setAccountValueCallBack setAccountBack;


        //定义回调的使用方法
        private void SetValue(string value)
        {
            this.text.Text = value;
        }
        private void SetAccountValue(string value) {
            this.accounts.Text = value;
        }

        private void SetSliderValue(long value) {
            this.slider1.Value = value;
        }
        /*
        private void SetAppendValue(string value)
        {
            this.re_record.Text = this.re_record.Text + value;
        }*/
        private void SetButtonValue(bool value)
        {
            this.PrivateButton1.IsEnabled = value;
        }
        private void Private_Click(object sender, RoutedEventArgs e)
        {
            //Async_HTTP_Search((int)BLOCK_Flag.priv);
            start_Search_Task((int)BLOCK_Flag.priv);
        }
        private void start_Search_Task(int flag) {
            Task taskSearch = new Task(() =>
            {
                Async_HTTP_Search(flag);

            });
            taskSearch.Start();
        }

        private void old_Private_Click(object sender, RoutedEventArgs e)
        {
            //this.text.Text = getBlockData(0);

            _DEBUG_FLAG = false;
            listBox.Items.Clear();
            DateTime start = DateTime.Now;
            PrivateButton1.IsEnabled = false;
            //re_record.Text = "";
            AccountNEOs.Clear();
            AccountGASs.Clear();

            //re_record.VerticalScrollBarVisibility = true;

            //实例化回调
            setCallBack = new setTextValueCallBack(SetValue);
            setSpliderBack = new setSliderValueCallBack(SetSliderValue);
            //setAppendBack = new setTextAppendCallBack(SetAppendValue);
            setButtonBack = new setButtonValueCallBack(SetButtonValue);
            setAccountBack = new setAccountValueCallBack(SetAccountValue);

            //all_tx_block.Clear();
            //all_tx_block.Append("{record:[");

            long bid = Convert.ToInt64(this.blockid.Text.ToString());
            slider1.Maximum = bid;
            Task mytask = new Task(
                () => {
                    for(long bi =0;  bi <= bid; bi ++){

                        //if (bi > 0 ) bi = 476;
                       
                        string resultstr = getBlockData(bi);
                        Console.WriteLine("index:" + bi);
                        //使用回调
                        slider1.Dispatcher.Invoke(setSpliderBack,bi);
                        //使用回调
                        //text.Dispatcher.Invoke(setCallBack, resultstr);
                        text.Dispatcher.Invoke(setCallBack, bi.ToString());

                        string record_str =  doWorkTxData(resultstr,bi,(int)BLOCK_Flag.priv );
                        if (!string.IsNullOrEmpty(record_str))
                        {

                            //all_tx_block.Append(record_str);
                            //all_tx_block.Append(","); record_str = "\nindex:" + bi + "\n" + record_str;
                            //re_record.Dispatcher.Invoke(setAppendBack, record_str);
                            this.Dispatcher.Invoke(()=> {
                                this.listBox.Items.Add(bi);
                            });
                        }
                    }
                    /*
                    this.Dispatcher.Invoke(
                        () => {
                            this.re_record.Text = resultstr;
                        }
                        );
                    */
                    PrivateButton1.Dispatcher.Invoke(setButtonBack, true);
                    DateTime end = DateTime.Now;
                    var doTime = (end - start).TotalSeconds;//.TotalMilliseconds;
                    string timestr = "耗时:" + doTime + "s\n";
                    accounts.Dispatcher.Invoke(setAccountBack, timestr + totalAccountRecord());

                }
                
            );
            mytask.Start();
        }
        private string totalAccountRecord()
        {
            string result = "";
            double neot = 0, gast = 0.0;
            foreach (KeyValuePair<string, double> kvp in AccountNEOs)
            {
                result += "{" + kvp.Key + "},NEO = " + kvp.Value + "\n";
                neot += kvp.Value;
            }
            result += "Total NEO=" + neot +  "\n\n";
            foreach (KeyValuePair<string, double> kvp in AccountGASs)
            {
                result += "{"+ kvp.Key  + "},GAS = "+ kvp.Value + "\n";
                gast += kvp.Value;
            }
            result += "Total GAS=" + gast + "\n";
            return result;
        }


        //保存 账号信息到 JSON 文件====
        private void  saveAccountRecord(long blockIndex ,int flag )
        {
            //string result = "";
            //double neot = 0, gast = 0.0;

            string filename = "";

            if (flag ==(int) BLOCK_Flag.main) filename = mainDataDirection + "Address\\";
            if (flag == (int)BLOCK_Flag.test) filename = testDataDirection + "Address\\";
            if (flag == (int)BLOCK_Flag.priv) filename = privateDataDirection + "Address\\";
            if (!Directory.Exists(filename)) Directory.CreateDirectory(filename);
            filename = filename + blockIndex;
            JArray neolist = new JArray();
            JArray gasList = new JArray();

            foreach (KeyValuePair<string, double> kvp in AccountNEOs)
            {
                //result += "{" + kvp.Key + "},NEO = " + kvp.Value + "\n";
                //neot += kvp.Value;
                neolist.Add(new JObject { { "ADDRESS", kvp.Key }, {"VALUE",kvp.Value  } });


            }
            //result += "Total NEO=" + neot + "\n\n";
            foreach (KeyValuePair<string, double> kvp in AccountGASs)
            {
                //result += "{" + kvp.Key + "},GAS = " + kvp.Value + "\n";
                //gast += kvp.Value;
                gasList.Add(new JObject { { "ADDRESS", kvp.Key }, { "VALUE", kvp.Value } });
            }
            //result += "Total GAS=" + gast + "\n";
            JObject node = new JObject();
            node.Add("neo", neolist);
            node.Add("gas", gasList);
            //string p = @"..\..\NewJson\Create.json";
            //found the file exist 
            if (!File.Exists(filename))
            {
                FileStream fs1 = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite);
                fs1.Close();
            }
            //write the json to file 
            File.WriteAllText(filename, node.ToString());
        }
        //从历史数据中恢复账户数据 ======
        private long loadAccountRecord(int flag, long blockIndex)
        {
            string folderFullName = "";

            if (flag == (int)BLOCK_Flag.main) folderFullName = mainDataDirection + "Address\\";
            if (flag == (int)BLOCK_Flag.test) folderFullName = testDataDirection + "Address\\";
            if (flag == (int)BLOCK_Flag.priv) folderFullName = privateDataDirection + "Address\\";
            if (!Directory.Exists(folderFullName)) Directory.CreateDirectory(folderFullName);

            List<long> fileNames = new List<long>();

            DirectoryInfo TheFolder = new DirectoryInfo(folderFullName);
            //遍历文件夹
            //foreach (DirectoryInfo NextFolder in TheFolder.GetDirectories())
            //    this.listBox1.Items.Add(NextFolder.Name);
            //遍历文件
            foreach (FileInfo NextFile in TheFolder.GetFiles())
                fileNames.Add(long.Parse( NextFile.Name));
            fileNames.Sort();
            foreach(long i in fileNames)
            {
                Console.WriteLine("filenames:"+i);
            }
            //查找满足条件的最后一个数 ======
            long listFind = fileNames.FindLast((x) => { return x <= blockIndex; } );
            Console.WriteLine("listFind:" + listFind);


            string filename = folderFullName + listFind;
            if (!File.Exists(filename)) {
                return -1;
            }

            var sourceContent = File.ReadAllText(filename);
            //parse as array  
            //var sourceobjects = JArray.Parse("[" + sourceContent + "]");
            JObject source = JObject.Parse(sourceContent);
            var neos = source["neo"];
            foreach (var neo in neos){
                AccountNEOs[neo["ADDRESS"].ToString()] = double.Parse(neo["VALUE"].ToString());

            }
            var gases = source["gas"];
            foreach (var gas in gases)
            {
                AccountGASs[gas["ADDRESS"].ToString()] = double.Parse(gas["VALUE"].ToString());

            }
            return listFind;
        }


        private string getBlockData(long doIndex)
        {
            //获取Cli block数据
            string resBlock = GetNeoCliData("getblock", new object[]
                {
                    doIndex,
                    1
                });
            //update_block();
            return resBlock;
        }
        private static string GetNeoCliData(string method, object[] paras)
        {
            Uri rpcEndpoint = new Uri(NeoCliJsonRPCUrl);
            JsonRpcWebClient rpc = new JsonRpcWebClient(rpcEndpoint);


            var response = rpc.InvokeAsync<JObject>(method, paras);
            JObject resJ = JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(response.Result));
            var resStr = Newtonsoft.Json.JsonConvert.SerializeObject(resJ["result"]);

            return resStr;
        }
      

        //、、查找存在交易数量发生变化的BLOCK
        private  string doWorkTxData(string blockdata, long blockindex, int flag) {
            //BsonDocument queryB = blockdata.ToBsonDocument();
            //BsonArray Txs = queryB["tx"].AsBsonArray;
            //
            //var obj =  blockdata.ToJson();

            Console.WriteLine("doWorkTxData: " + blockindex);
            //if (_DEBUG_FLAG) Console.WriteLine("blockdate ==  ",blockdata);
            var Txs = JObject.Parse(blockdata)["tx"];
            //if (_DEBUG_FLAG)  Console.WriteLine("Txs === "+Txs.ToString());
            int si = 0;
            string tx_record = "";
            foreach (JObject bv in Txs)
            {

                if (_DEBUG_FLAG)
                {
                    Console.WriteLine("BV=== "+bv.ToString());
                }

                var Vin = bv["vin"].ToList();
                var Vout = bv["vout"].ToList();
                bool findInOut = false;
                //存在交易记录========
                if ( Vin.Count>0  || Vout.Count > 0 )
                {
                    findInOut = true;
                    string DataDirection = mainDataDirection;
                    if (flag ==(int)BLOCK_Flag.test) {

                        DataDirection = testDataDirection;
                    }
                    if (flag == (int)BLOCK_Flag.priv)
                    {

                        DataDirection = privateDataDirection;
                    }
                    //保存交易数据到日志文件，用于INPUT查询 OUTX的账单数据
                    if (!Directory.Exists(DataDirection))
                    {
                        Directory.CreateDirectory(DataDirection);
                    }
                    string txid = bv["txid"].ToString();
                    WriteStringToFile(bv.ToString(), DataDirection + txid, false);


                }

                si++;
                //var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                //JObject Tx = JObject.Parse(bv.ToJson(jsonWriterSettings));
                //DoWorkUTXOData(bv);
                if (findInOut)
                {
                    //Console.WriteLine("***********( " + si + " )======");
                    //Console.WriteLine(bv);
                    DoWorkUTXOData(bv, blockindex,flag);
                    tx_record += bv;
                }
            }
            //Console.WriteLine("********* <  end  >  *********");
            return tx_record;
        }


        //计算每笔交易余额
        private  void DoWorkUTXOData(JObject Tx ,long blockindex , int flag)
        {
            //DoWorkUTXOData(Tx);
            Console.WriteLine("DoWorkUTXOData:" + blockindex);

            //string allTxBlock = all_tx_block.ToString()+"]}";
            //var allTxs = JObject.Parse(allTxBlock)["record"];
            string DataDirection = "";
            if (flag == (int)BLOCK_Flag.main) DataDirection = mainDataDirection;
            if (flag == (int)BLOCK_Flag.test) DataDirection = testDataDirection;
            if (flag == (int)BLOCK_Flag.priv) DataDirection = privateDataDirection;

            var Vins = Tx["vin"].ToList();
            var Vouts = Tx["vout"].ToList();

            int vins_index = 1; 


            //处理抛出的交易数量
            if (Vins.Count > 0)
            {
                //当输入记录数超过1000时，显示进度%
                bool showJD = Vins.Count >= 1000 ? true : false;
                int between = (int)Vins.Count / 100;
                between = between < 1 ? 1 : between;
                //Console.WriteLine("%%%%%  vin %%%%%%%%%%%");
                //Console.WriteLine(Tx["vin"]);
                // Console.WriteLine("###########  allTxs  ###########");
                //逐条 处理 INPUT的交易记录
                string LastTxid = "";
                string LastFilestr = "";

                foreach (JObject vin in Tx["vin"])
                {
                    //Console.WriteLine(vin["vout"].ToString());
                    //var mj = allTxs["txid"];
                    //Console.WriteLine(mj.ToString());

                    // allTxs["vout"][0]["n"].ToString() == vin["vout"].ToString();

                    //全量查找OUTPUT交易记录中的交易号 txid
                    //var result = allTxs.ToList().Where(p => p["txid"].ToString() == vin["txid"].ToString()).Select(p => p["vout"]) ;//&& p["vout"]["n"].ToString() == vin["vout"].ToString());// .Where(p => p["txid"]==vin["txid"] && p["vout"]["n"]==vin["vout"]).Select(p => p["out"]["asset"]);
                    string filestr = "";
                    if (LastTxid != vin["txid"].ToString())
                    {
                        LastTxid = vin["txid"].ToString();
                        filestr = ReadFileToString(DataDirection + LastTxid).ToString();
                        LastFilestr = filestr;
                    }
                    else
                        filestr = LastFilestr;

                    //if (String.IsNullOrEmpty(filestr)) continue;
                    if (_DEBUG_FLAG)
                    {
                        if (String.IsNullOrEmpty(filestr)) Console.WriteLine(DataDirection + vin["txid"].ToString() + " 不存在!");
                        
                        Console.WriteLine("Block index:" + blockindex +  " 文件名："+ vin["txid"].ToString() +  " filestr ==" + filestr);
                        //Thread.Sleep(2000);
                            
                    }
                    //Console.WriteLine("Block index:" + blockindex + " 文件名：" + vin["txid"].ToString() + " filestr ==" + filestr);

                    if (String.IsNullOrEmpty(filestr))
                    {
                        this.Dispatcher.Invoke(()=> {
                            this.errorText.Text = this.errorText.Text + "Block index:" + blockindex + " "+ DataDirection + vin["txid"].ToString() + " 不存在!!!\n\r";
                            this.errorText.Text = this.errorText.Text +"\n\r[vin]\n\r"+ vin.ToString() + "\n\r";
                            this.errorText.Text = this.errorText.Text + "\n\r[vout]\n\r" + Tx["vout"].ToString() + "\n\r";
                        });
                        continue;
                    }

                    //JsonReader reader = new JsonTextReader(filestr);
                    //Console.WriteLine(filestr.ToJson());
                    //var resultstr = (JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(filestr);
                    var resultstr = JObject.Parse(filestr);
                    var result = resultstr["vout"];
                    
                    Console.WriteLine("  Index:" + blockindex + " vins(" + vins_index + "/"+ Vins.Count + ") "+  vin["txid"].ToString() + " " + vin["vout"].ToString());
                    vins_index++;
                    if (showJD)
                    {
                        if (vins_index % between == 0)
                            this.Dispatcher.Invoke(() => {
                                this.text.Text = blockindex + " " + (int)(vins_index *100 / Vins.Count) + "%";
                            });
                    }

                    //逐条 查找 OUTPUT交易记录 ===
                    foreach (var  v in result)
                    {
                        //Console.WriteLine(ci.ToString() + " p &&& " + p.Children());
                        //"Children()"可以返回所有数组中的对象
                        //var v = p; //  p.Children(); // JArray.Parse(p.ToString());

                        //查找 OUTPUT交易记录中的参数
                        //foreach (var v in m) {
                            if (v["n"].ToString() == vin["vout"].ToString())
                            {
                                //Console.WriteLine(ci.ToString() + " v &&& " + v["n"] + " " + v["asset"] + " " + Double.Parse(v["value"].ToString()) + " " + v["address"]);
                                //提供交易资源，删除交易数量
                                UpdateUserAccount(v,false);
                                break;
                            }

                        //}
                       
                    }
 
                }
                
            }

            //处理收入的交易数量
            if (Vouts.Count > 0)
            {
                foreach (JObject vout in Tx["vout"])
                {
                    //收获交易，增加资产数量
                    UpdateUserAccount(vout, true);
                }

            }
            Console.WriteLine("$$$$$$$$$$$$$$$$$$$$$$$$$$");

        }
        //修改用户的交易数量
        private void UpdateUserAccount(JToken value,bool addFlag)
        {
            //var v = value;
            //Console.WriteLine( v["n"] + " " + v["asset"] + " " + Double.Parse(v["value"].ToString()) + " " + v["address"]);
            //List  address = AccountRecord[v["address"].ToString()];
            string key = value["address"].ToString();
            string asset = value["asset"].ToString();
            double neo=0.0, gas=0.0;
            bool neo_flag = false;
            if (asset == "0xc56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b") {
                neo = Double.Parse(value["value"].ToString());
                neo_flag = true;
            }
            if (asset == "0x602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7") {
                gas = Double.Parse(value["value"].ToString());
            }
            if (neo_flag)//如果交易类型为neo ====
            {
                //判断是否存在 当前用户 ======
                if (AccountNEOs.ContainsKey(key))
                {
                    double curr_neo = AccountNEOs[key];
                    
                    if (addFlag) //增加收入 
                        AccountNEOs[key] = curr_neo + neo;
                    else  //删除收入
                        AccountNEOs[key] = curr_neo - neo;
                }
                else
                {
                    if (addFlag)
                        AccountNEOs.Add(key, neo);
                    else
                        AccountNEOs.Add(key, -neo);
                }
                /*
                if(_DEBUG_FLAG)
                    foreach (KeyValuePair<string, double> kvp in AccountNEOs)
                    {
                        Console.WriteLine("Key = {0}, NEO = {1}", kvp.Key, kvp.Value);
                    }*/

            }
            else { //交易类型为gas
                if (AccountGASs.ContainsKey(key))
                {
                    double curr_gas = AccountGASs[key];
                    if (addFlag)
                        AccountGASs[key] = curr_gas + gas;
                    else
                        AccountGASs[key] = curr_gas - gas;
                }
                else
                {
                    if (addFlag)
                        AccountGASs.Add(key, gas);
                    else
                        AccountGASs.Add(key, -gas);
                }
                /*
                if(_DEBUG_FLAG)
                    foreach (KeyValuePair<string, double> kvp in AccountGASs)
                    {
                        Console.WriteLine("Key = {0}, GAS = {1}", kvp.Key, kvp.Value);
                    }
                */

            }


        }
        /*
        async void update_block()
        {
            try
            {
                var height = await api_block();
                //apiHeight = height;
                this.Dispatcher.Invoke(() =>
                {
                    this.text.Text = "height=" + height;
                });
            }
            catch
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.text.Text = "offline";
                });
            }
            //try
            //{
            //    var height = await rpc_getHeight();
            //    rpcHeight = height;
            //    this.Dispatcher.Invoke(() =>
            //    {
            //        this.stateRPC.Text = "height=" + height;
            //    });
            //}
            //catch
            //{
            //    this.Dispatcher.Invoke(() =>
            //    {
            //        this.stateRPC.Text = "offline";
            //    });
            //}
        }
        async Task<ulong> api_block()
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            //var array = new List<int>();
            // array.Add(0);
            // array.Add(1);

            var str = WWW.MakeRpcUrl(NeoCliJsonRPCUrl, "getblockcount");
            var result = await wc.DownloadStringTaskAsync(str);
            var json = MyJson.Parse(result).AsDict()["result"].AsList();
            var height = ulong.Parse(json[0].AsDict()["blockcount"].ToString()) - 1;
            return height;
        }
        */
        private void Test_Button_Click(object sender, RoutedEventArgs e)
        {
           
            //Async_HTTP_Search((int)BLOCK_Flag.test);
            start_Search_Task((int)BLOCK_Flag.test);
        }

        private void Main_button1_Click(object sender, RoutedEventArgs e)
        {
            //Async_HTTP_Search((int)BLOCK_Flag.main);
            start_Search_Task((int)BLOCK_Flag.main);
        }

        // HTTPS获取BLOCK COUNT数据 ===
        private async Task<string> HttpGetBlockCount( int flag)
        {
            string result = null;
            while (true)
            {
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Encoding = System.Text.Encoding.GetEncoding("UTF-8");  //防止中文乱码
                                                                                  //Uri uri = new Uri("https://www.baidu.com");
                                                                                  //result = wc.DownloadString(uri);//.   DownloadStringTaskAsync("https://www.baidu.com"));

                        string urlstr = null;
                        if (flag == (int)BLOCK_Flag.test)
                            urlstr = NeoHTTPTestUrl + "?jsonrpc=2.0&id=1&method=getblockcount&params=[]";

                        if (flag == (int)BLOCK_Flag.main)
                            urlstr = NeoHTTPMainUrl + "?jsonrpc=2.0&id=1&method=getblockcount&params=[]";

                        if (flag == (int)BLOCK_Flag.priv)
                            urlstr = NeoCliJsonRPCUrl + "?jsonrpc=2.0&id=1&method=getblockcount&params=[]";

                        result = await wc.DownloadStringTaskAsync(urlstr);
                        break;
                    }

                }
                catch (Exception e)
                {
                    this.Dispatcher.Invoke(() => {
                        if (flag == (int)BLOCK_Flag.test)
                            this.errorText.Text = this.errorText.Text + "test BlockCount ERROR:" + e.ToString() + " \n\r";
                        if (flag == (int)BLOCK_Flag.main)
                            this.errorText.Text = this.errorText.Text + "main BlockCount ERROR:" + e.ToString() + " \n\r";
                        if (flag == (int)BLOCK_Flag.priv)
                            this.errorText.Text = this.errorText.Text + "private BlockCount ERROR:" + e.ToString() + " \n\r";
                    });
                    Thread.Sleep(3000);
                }
            }
            return result;
        }


        // HTTPS获取BLOCK数据 ===
        private async Task<string> HttpGetBlock(string index, int flag)
        {
            string result = null;
            while (true)
            {
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Encoding = System.Text.Encoding.GetEncoding("UTF-8");  //防止中文乱码
                                                                                  //Uri uri = new Uri("https://www.baidu.com");
                                                                                  //result = wc.DownloadString(uri);//.   DownloadStringTaskAsync("https://www.baidu.com"));

                        string urlstr = null;
                        if (flag == (int)BLOCK_Flag.test)
                            urlstr = NeoHTTPTestUrl + "?jsonrpc=2.0&id=1&method=getblock&params=[" + index + ",1]";

                        if (flag == (int)BLOCK_Flag.main)
                            urlstr = NeoHTTPMainUrl + "?jsonrpc=2.0&id=1&method=getblock&params=[" + index + ",1]";

                        if (flag == (int)BLOCK_Flag.priv)
                            urlstr = NeoCliJsonRPCUrl + "?jsonrpc=2.0&id=1&method=getblock&params=[" + index + ",1]";

                        result = await wc.DownloadStringTaskAsync(urlstr);
                        break;
                    }

                }
                catch (Exception e)
                {
                    this.Dispatcher.Invoke(() => {
                        if (flag == (int)BLOCK_Flag.test)
                            this.errorText.Text = this.errorText.Text + "test Block Index:" + index + " ERROR:" + e.ToString() + " \n\r";
                        if (flag == (int)BLOCK_Flag.main)
                            this.errorText.Text = this.errorText.Text + "main Block Index:" + index + " ERROR:" + e.ToString() + " \n\r";
                        if (flag == (int)BLOCK_Flag.priv)
                            this.errorText.Text = this.errorText.Text + "private Block Index:" + index + " ERROR:" + e.ToString() + " \n\r";
                    });
                    Thread.Sleep(3000);
                }
            }
            return result;
        }
        private void refreshListData(int flag)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.listBox.Items.Clear();
                if (flag == (int)BLOCK_Flag.test)
                {
                    TestButton.IsEnabled = false;
                    string last = ReadTxtToLstLOCK(listBox, testFileName,flag);
                    if(long.Parse(last) != test_Index) this.listBox.Items.Add(test_Index);
                }
                if (flag == (int)BLOCK_Flag.main)
                {
                    MainButton.IsEnabled = false;
                    string last = ReadTxtToLstLOCK(listBox, mainFileName,flag);
                    if (long.Parse(last) != main_Index) this.listBox.Items.Add(main_Index);
                }
                if (flag == (int)BLOCK_Flag.priv)
                {
                    PrivateButton1.IsEnabled = false;
                    string last = ReadTxtToLstLOCK(listBox, privateFileName,flag);
                    if (long.Parse(last) != private_Index) this.listBox.Items.Add(private_Index);
                }


            }
            );

        }

        private  void getListFile(int flag) {

            Task reflashTask = new Task( ()=> {
                reqNew.newRequest = true;
                reqNew.request_Block_Flag = flag;
                while (true)
                {
                    Thread.Sleep(1500);
                    Console.WriteLine("getListFile....");
                    if (!reqNew.newRequest)
                    {
                        Console.WriteLine("reflash dataList....");
                        break;
                    }
                }
            });
            reflashTask.Start();
            /*
            await Task.Run(() => {
                reqNew.newRequest = true;
                reqNew.request_Block_Flag = flag;
                while (true)
                {
                    Thread.Sleep(1500);
                    Console.WriteLine("getListFile....");
                    if (!reqNew.newRequest)
                    {
                        Console.WriteLine("reflash dataList....");
                        break;
                    }
                }
            });
            Console.WriteLine("getListFile ok!!!");
            */

        }
        //
        private void saveERRORlog(int flag)
        {
            string logf = "";
            if (flag == (int)BLOCK_Flag.main ) logf = mainDataDirection +@"log\";
            if (flag == (int)BLOCK_Flag.test) logf = testDataDirection + @"log\";
            if (flag == (int)BLOCK_Flag.priv) logf = privateDataDirection + @"log\";
            string errstr = null;
            this.Dispatcher.Invoke(() =>
            {
                errstr = this.errorText.Text.ToString().Trim();
            }
            );

            if (!string.IsNullOrEmpty(errstr))
            {
                if (!Directory.Exists(logf)) Directory.CreateDirectory(logf);
                //DateTime cnow = DateTime.Now;
                logf = logf + "ERROR_log_" + DateTime.Now.ToString("u").Replace(":", "-")+ ".txt";// cnow.ToShortDateString() + " " + cnow.ToShortTimeString();

                WriteStringToFile(errstr, logf, false);
            }
        }

        //= 开始执行HTTP快速查询任务
        private async void Async_HTTP_Search(int flag)
        {
            string helpstr="";
            _DEBUG_FLAG = false;
            long bid = 0;
            this.Dispatcher.Invoke(() => {
                bid = long.Parse(this.blockid.Text.ToString());
                this.slider1.Maximum = bid;
                this.errorText.Text = "";
            });

            //导入上次处理的BLOCK INDEX数据，到缓冲区
            refreshListData(flag);


           

            DateTime start = DateTime.Now;
            this.Dispatcher.Invoke(() =>
            {
                if (flag == (int)BLOCK_Flag.test)
                {
                    //TestButton.IsEnabled = false;
                    helpstr = "Test Chain :" + this.blockid.Text.ToString();
                    //ReadTxtToLst(listBox, testFileName);
                }
                if (flag == (int)BLOCK_Flag.main)
                {
                    //MainButton.IsEnabled = false;
                    helpstr = "Main Chain :" + this.blockid.Text.ToString();
                    //ReadTxtToLst(listBox, mainFileName);
                }

                if (flag == (int)BLOCK_Flag.priv)
                {
                    //PrivateButton1.IsEnabled = false;
                    helpstr = "Private Chain :" + this.blockid.Text.ToString();

                    //ReadTxtToLst(listBox, testFileName);
                }
            });
            //钱包的资产数据缓冲区
            AccountNEOs.Clear();
            AccountGASs.Clear();


            long saveBi =  loadAccountRecord(flag, bid);

            long bi = 0;
            int countI = 0;
            // 对缓冲区的BLOCK INDEX 进行分析处理，提升了查询速度 ===
            for (int i = 0; i < listBox.Items.Count  ; i++) {
                string index = "";
                this.Dispatcher.Invoke(() =>
                {
                    index = listBox.Items[i].ToString().Trim();
                }
                );
                //if (string.IsNullOrEmpty(index)) break;
                if (index.IndexOf("#") > -1) continue;

                bi = long.Parse(index);
                if (bi <= saveBi) continue;

                //每次20个交易记录保存一次文件 ====
                countI++;
                if (countI >= 20) {
                    saveAccountRecord(bi, flag);
                    countI = 0;
                }
                /*
                if (bi < 46148)
                {
                    bi = 46148;
                    _DEBUG_FLAG = true;
                }*/
                if (bi > bid)  //找到 index 退出
                {
                  
                    this.Dispatcher.Invoke(()=>{
                        this.slider1.Value = bid;
                    });
                    break;
                }
                await DoWorkBlockDate(bi, flag);
                //Thread.Sleep(1000);
            }
            /*
            // 循环 查找 文件 一直等到 出现 需要查询到的 INDEX
            while (bi < bid)
            {
                int currListIndex = listBox.Items.Count;
                //重新导入数据

                //getListFile(flag,currListIndex);
               

                getListFile(flag);
                for (int i = currListIndex; i < listBox.Items.Count - 1; i++)
                {
                    string index = listBox.Items[i].ToString().Trim();
                    //if (string.IsNullOrEmpty(index)) break;
                    if (index.IndexOf("#") > -1) continue;

                    bi = Convert.ToInt64(index);

                    if (bi > bid) //找到 index 退出 
                    {
                        break;
                    }
                    await DoWorkBlockDate(bi, flag);
                    //Thread.Sleep(1000);
                }
                if (bi > bid)  //找到 index 退出
                {

                    this.Dispatcher.Invoke(() => {
                        this.slider1.Value = bid;
                    });
                    break;
                }
            }*/


            //判断上次处理的最后一个记录 =====
            /*
            long last_bid = 0;
            if (listBox.Items.Count > 0) {
                string index = listBox.Items[listBox.Items.Count-1].ToString().Trim();
                if (string.IsNullOrEmpty(index)) last_bid = bi+1;
                last_bid = Convert.ToInt64(index);
                if (last_bid > bi) bi = last_bid;
                else bi++;
                listBox.Items.Remove(index);
            }*/

            //判断上次处理的最后一个记录 =====
            string sindex = listBox.Items[listBox.Items.Count - 1].ToString().Trim();
            if(sindex.IndexOf("#") > -1)
            {
                sindex = sindex.Substring(1);
                long last_bid = Convert.ToInt64(sindex);
                if (last_bid > bi) bi = last_bid;
            }
            bi++;
            for (; bi <= bid; bi++)
            {
                bool addL = await DoWorkBlockDate(bi, flag);
                //增加有数据的BLOCK INDEX到缓冲区
                if (addL) {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.listBox.Items.Add(bi);
                    });

                    //每次20个交易记录保存一次文件 ====
                    countI++;
                    if (countI >= 20)
                    {
                        saveAccountRecord(bi, flag);
                        countI = 0;
                    }
                }
               
            }
            /*
            //增加处理的数据记录的下一条Block Index 到缓冲区
            if (bid < last_bid)
                this.listBox.Items.Add(last_bid); 
            else
                this.listBox.Items.Add(bid+1);
            //保存有交易的数据块号 到文件
            if (flag == (int)BLOCK_Flag.test)
                WriteLstToTxt(listBox, testFileName);
            if (flag == (int)BLOCK_Flag.main)
                WriteLstToTxt(listBox, mainFileName);
            */

            this.Dispatcher.Invoke(() => {
                this.slider1.Value = bid;
                this.TestButton.IsEnabled = true;
                this.MainButton.IsEnabled = true;
                this.PrivateButton1.IsEnabled = true;

            });
            DateTime end = DateTime.Now;
            var doTime = (end - start).TotalSeconds;//.TotalMilliseconds;
            string timestr = "耗时:" + doTime + "s\n";
            this.Dispatcher.Invoke(()=> {
                accounts.Text = helpstr +"\n" +timestr + totalAccountRecord();

            });
            saveERRORlog(flag);
            saveAccountRecord(bid, flag);


            //accounts.Dispatcher.Invoke(setAccountBack, timestr + totalAccountRecord());

        }

        //查找 一条记录的是否存在 交易记录 ====
        private bool find_ONE_TX_INorOUT(string resultstr, int flag,long bi)
        {
            //resultstr = r;
            bool findInOut = false;
            var Txs = JObject.Parse(resultstr)["tx"];

            foreach (JObject bv in Txs)
            {
                var Vin = bv["vin"].ToList();
                var Vout = bv["vout"].ToList();
                //存在交易记录========
                if (Vin.Count > 0 || Vout.Count > 0)
                {
                    findInOut = true;
                    break;
                }
            }
            return findInOut;
        }

        private async Task<long> FindBlockCount(int flag)
        {
            string result = await HttpGetBlockCount(flag);
            JObject resJ = JObject.Parse(result);
            JArray res = null;
            if (flag == (int)BLOCK_Flag.priv) //采集 privatenet{
            {
                result = resJ["result"].ToString();
            }
            else {
                res = JArray.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(resJ["result"]));
                //Console.WriteLine(res[0]["blockcount"]);
                result = res[0]["blockcount"].ToString();
            }
            return long.Parse(result);


        }


            // 搜索BLOCK的数据是否存在交易记录，返回INDEX  searchTest = TRUE，表示使用测试链 FALSE 使用公有链
            private async Task<int> FindINorOUTdata(long bi, int flag)
        {
            bool findInOut = false;
            string result = await HttpGetBlock(bi.ToString(), flag);
            JObject resJ = JObject.Parse(result);
            JArray res = null;
            if (flag == (int)BLOCK_Flag.priv) //采集 privatenet
            {
                try
                {
                    findInOut = find_ONE_TX_INorOUT(resJ["result"].ToString(), flag, bi);
                }
                catch (Exception e) {
                    Console.WriteLine("Private block index:" + bi + " " + e.ToString() + resJ);
                    var errres = resJ["error"];
                    return -1;
                }
            }
            else  // 采集  mainnet  testnet
            {
                try
                {
                    res = JArray.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(resJ["result"]));
                }
                catch (Exception e)
                {

                    Console.WriteLine("block index:" + bi + " " + e.ToString() + resJ);
                    var errres = resJ["error"];
                    return -1;

                }

                foreach (var r in res)
                {

                    findInOut = find_ONE_TX_INorOUT(r.ToString(), flag, bi);
                    if (findInOut) break;
                    /*
                    resultstr = r.ToString();

                    var Txs = JObject.Parse(resultstr)["tx"];
                    if (flag == (int)BLOCK_Flag.test)
                    {
                        Console.WriteLine("test: " + bi);
                        this.Dispatcher.Invoke(() =>
                        {
                            this.TestIndex.Content = bi;
                        });
                    }
                    if (flag == (int)BLOCK_Flag.main)
                    {
                        Console.WriteLine("main: " + bi);
                        this.Dispatcher.Invoke(() =>
                        {
                            this.MainIndex.Content = bi;
                        });
                    }
                    if (flag == (int)BLOCK_Flag.priv)
                    {
                        Console.WriteLine("private: " + bi);
                        this.Dispatcher.Invoke(() =>
                        {
                            this.PrivateIndex.Content = bi;
                        });
                    }
                    foreach (JObject bv in Txs)
                    {
                        var Vin = bv["vin"].ToList();
                        var Vout = bv["vout"].ToList();
                        //存在交易记录========
                        if (Vin.Count > 0 || Vout.Count > 0)
                        {
                            findInOut = true;
                            break;
                        }
                    }*/
                }
            }
            return findInOut?1:0;
        }
        // 处理 一条TX记录数据
        private bool DoOneTXData(string resultstr ,long bi,int flag)
        {
            //resultstr = r.ToString();
            bool addList = false;
            if (_DEBUG_FLAG)
            {
                Console.WriteLine(resultstr);
            }
            Console.WriteLine("DoOneTXData");
            string record_str = doWorkTxData(resultstr, bi, flag);
            if (!string.IsNullOrEmpty(record_str))
            {
                //保存交易数据到缓冲区，用于INPUT查询 OUTX的账单数据
                Console.WriteLine("保存交易数据到缓冲区，用于INPUT查询 OUTX的账单数据");
                record_str = "\nindex:" + bi + "\n" + record_str;
                // 发现新的记录， 可增加listbox 的标记 
                addList = true;
            }
            return addList;
        }

        // 处理 BLOCK数据 
        private async Task<bool> DoWorkBlockDate(long bi, int flag)
        {
            //if(bi >=6000) _DEBUG_FLAG = true;

            string result = await HttpGetBlock(bi.ToString(), flag);

            Console.WriteLine("###  DoWorkBlockDate :" + bi);

            if (_DEBUG_FLAG)
            {
                string DataDirection = "";
                if (flag == (int)BLOCK_Flag.main) DataDirection = mainDataDirection;
                if (flag == (int)BLOCK_Flag.test) DataDirection = testDataDirection;
                if (flag == (int)BLOCK_Flag.priv) DataDirection = privateDataDirection;
                if (!Directory.Exists(DataDirection + @"log\")) Directory.CreateDirectory(DataDirection + @"log\");
                WriteStringToFile(result.ToJson(), DataDirection + @"log\" + bi, false);
            }

            bool addList = false;
            JObject resJ = JObject.Parse(result);
            //JObject resJ = JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(result.Result));

            if (flag == (int)BLOCK_Flag.priv)
            {
                //var r = resJ["result"];
                addList = DoOneTXData(resJ["result"].ToString(), bi, flag);
            }
            else
            {

                JArray res = JArray.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(resJ["result"]));
                //string resultstr;

                foreach (var r in res)
                {
                    //resultstr = r.ToString();
                    addList = DoOneTXData(r.ToString(), bi, flag);
                    /*
                    if (_DEBUG_FLAG)
                    {
                        Console.WriteLine(resultstr);
                    }
                    string record_str = doWorkTxData(resultstr, bi, flag);
                    if (!string.IsNullOrEmpty(record_str))
                    {
                        //保存交易数据到缓冲区，用于INPUT查询 OUTX的账单数据
                        Console.WriteLine("保存交易数据到缓冲区，用于INPUT查询 OUTX的账单数据");
                        record_str = "\nindex:" + bi + "\n" + record_str;
                        // 发现新的记录， 可增加listbox 的标记 
                        addList = true;
                    }*/
                }
            }
            this.Dispatcher.Invoke(() =>
            {
                this.text.Text = bi.ToString();
                this.slider1.Value = bi;
            });
            
            return addList;
            
        }

        private void WriteStringToFileLOCK(string data,string spath,bool appendFlag ,int flag )
        {

            if(flag ==(int)BLOCK_Flag.main )
                lock (mainLockFileObj)
                {
                    WriteStringToFile(data, spath, appendFlag);
                }
            if (flag == (int)BLOCK_Flag.test)
                lock (testLockFileObj)
                {
                    WriteStringToFile(data, spath, appendFlag);
                }
            if (flag == (int)BLOCK_Flag.priv)
                lock (privateLockFileObj)
                {
                    WriteStringToFile(data, spath, appendFlag);
                }

        }
        private void WriteStringToFile(string data, string spath, bool appendFlag)
        {
            StreamWriter _wstream = null;
            _wstream = new StreamWriter(spath, appendFlag); //true 追加的标记
            _wstream.Write(data);
            _wstream.WriteLine();
            _wstream.Flush();
            _wstream.Close();

        }

        private void WriteLstToTxt(ListBox lst, string spath) //listbox 写入txt文件
        {
            //判断 如果存在当前文件，记录数大于 当前文件的最大行数就写入文件，否则退出
            if (File.Exists(spath))
            {
                if (listBox.Items.Count < ReadTxtCounts(spath)) return;

            }


            int count = lst.Items.Count;
            {
                StreamWriter _wstream = null;
                _wstream = new StreamWriter(spath);

                for (int i = 0; i < count; i++)
                {
                    string data = lst.Items[i].ToString();
                    _wstream.Write(data);
                    _wstream.WriteLine();
                }
                _wstream.Flush();
                _wstream.Close();
            }
        }


        private string ReadTxtToLstLOCK(ListBox lst, string spath,int flag) //listbox 读取txt文件
        {
            string resultstr = "";
            if (!File.Exists(spath))
            {
                return resultstr;
            }
            lst.Items.Clear();
            if(flag == (int)BLOCK_Flag.main)
                lock (mainLockFileObj)
                {
                    resultstr = ReadTxtToLst(lst, spath);
                }
            if (flag == (int)BLOCK_Flag.test)
                lock (testLockFileObj)
                {
                    resultstr = ReadTxtToLst(lst, spath);
                }
            if (flag == (int)BLOCK_Flag.priv)
                lock (privateLockFileObj)
                {
                    resultstr = ReadTxtToLst(lst, spath);
                }
            return resultstr;
        }
        private string ReadTxtToLst(ListBox lst, string spath)
        {
            StreamReader _rstream = null;
            _rstream = new StreamReader(spath, System.Text.Encoding.UTF8);
            string line,resultstr = "";
            while ((line = _rstream.ReadLine()) != null)
            {
                lst.Items.Add(line);
                resultstr = line;
            }
            _rstream.Close();
            return resultstr;
        }



        private StringBuilder ReadFileToString(string spath) // 读取txt文件
        {
            StringBuilder result = new StringBuilder();
            if (!File.Exists(spath))
            {
                return result;
            }

            StreamReader _rstream = null;
            _rstream = new StreamReader(spath, System.Text.Encoding.UTF8);
            string line;
            while ((line = _rstream.ReadLine()) != null)
            {
                result.Append(line);
            }
            _rstream.Close();
            return result;
        }

        private int ReadTxtCounts(string spath) //listbox 读取txt文件的行数==
        {
            int counts = 0;
            if (!File.Exists(spath))
            {
                return counts;
            }
            //lock (mainLockFileObj)
            {
                StreamReader _rstream = null;
                _rstream = new StreamReader(spath, System.Text.Encoding.UTF8);
                string line;

                while ((line = _rstream.ReadLine()) != null)
                {
                    counts++;
                }
                _rstream.Close();
            }
            return counts;
        }
        // 
        private string ReadTxtLastLine(string spath) //listbox 读取txt文件的最后一行文本==
        {
           
            if (!File.Exists(spath))
            {
                return "#-1";
            }

            StreamReader _rstream = null;
            _rstream = new StreamReader(spath, System.Text.Encoding.UTF8);
            string line,LastLine="#-1";

            while ((line = _rstream.ReadLine()) != null)
            {
               
                LastLine = line;
            }
            _rstream.Close();
            return LastLine;
        }


        private void Private_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            blockid.Text = PrivateIndex.Content.ToString().Split('/')[0].Trim(); ;
        }

        private void Test_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            blockid.Text = TestIndex.Content.ToString().Split('/')[0].Trim(); ;
        }

        private void Main_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            blockid.Text = MainIndex.Content.ToString().Split('/')[0].Trim();
        }
    }
}
