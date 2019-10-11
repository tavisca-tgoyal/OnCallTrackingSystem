using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OnCallTrackingSystem.Models;

namespace OnCallTrackingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private string _connection;
        public ValuesController()
        {
            _connection= "SERVER=127.0.0.1;PORT=3306;DATABASE=oncalltracking;UID=root;PASSWORD=1596324780";
        }


        // GET api/values
        [HttpGet]
        public ActionResult Get()
        {
            string EmpJsonString = new WebClient().DownloadString("https://chatops.common.cnxloyalty.com/api/team");

            List<Employee> JsonData = JsonConvert.DeserializeObject<List<Employee>>(EmpJsonString);



            MySqlConnection mySqlConnection = new MySqlConnection(_connection);
            MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
            try
            {
                mySqlConnection.Open();

                for (int i = 0; i < JsonData.Count; i++)
                {
                    mySqlCommand.CommandText = "Insert into employee values('" + JsonData[i].Id + "','" + JsonData[i].FullName + "','" + JsonData[i].Email + "','" + JsonData[i].EmployeeId + "','" + JsonData[i].PhoneNumber + "')";
                    try
                    {
                        mySqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {

                    }

                }


            }
            catch (Exception ex)
            {
                // return Ok("dude, I did what you told me to do!!");
            }
            return Ok(JsonData);
        }

        [HttpGet("{callData}")]        
       public ActionResult Get(string callData)
        {
            const string Format = "http://logs.stage.oski.io/_plugin/kibana/elasticsearch/_msearch";

            var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(string.Format(Format));
            // Set the 'Method' property of the 'Webrequest' to 'POST'.
            myHttpWebRequest.Method = "POST";


            // Create a new string object to POST data to the Url.

            var json = getJson();
 
            //var encoding = new ASCIIEncoding();
            var dataInBytes = Encoding.ASCII.GetBytes(json);

            // Set the content type of the data being posted.
            myHttpWebRequest.ContentType = "application/json";
            myHttpWebRequest.ContentLength = dataInBytes.Length;
            myHttpWebRequest.Headers["kbn-version"] = "5.1.1";

            // Set the content length of the string being posted.


            var stream = myHttpWebRequest.GetRequestStream();
            stream.Write(dataInBytes, 0, dataInBytes.Length);

            Console.WriteLine("The value of 'ContentLength' property after sending the data is {0}", myHttpWebRequest.ContentLength);

            // Close the Stream object.
            stream.Close();
            var response = (HttpWebResponse)myHttpWebRequest.GetResponse();
            var responseStream = response.GetResponseStream();
            StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8);
            string valueFromResponse = readStream.ReadToEnd();
            JObject newJson = JObject.Parse(valueFromResponse);

            int count = Convert.ToInt32(newJson["responses"][0]["hits"]["total"]);
            var values = newJson["responses"][0]["hits"]["hits"];

            List<CallData> list = new List<CallData>();
            for (int i = 0; i < count; i++)
            {
                var employee_id = values[i]["_source"]["employee_id"].ToString();
                var call_action = values[i]["_source"]["call_action"].ToString();
                list.Add(new CallData() { employee_id = employee_id, call_action = call_action });
            }


            MySqlConnection mySqlConnection = new MySqlConnection(_connection);
            MySqlCommand mySqlCommand = mySqlConnection.CreateCommand();
            try
            {
                mySqlConnection.Open();

                for (int i = 0; i < list.Count; i++)
                {
                    int init = 0;
                    int ack =0;

                    if (list[i].call_action == "initiated")
                    {
                        init = 1;                        
                    }
                    else if(list[i].call_action== "acknowledged")
                    {                        
                        ack = 1;
                    }
                    
                    mySqlCommand.CommandText = "Insert into EmpInitAckData(EmployeeId,init,ack,date) values( '" + list[i].employee_id + "','" + init + "','" + ack + "','" + DateTime.Today.ToString("yyyy-MM-dd") + "')";
                    try
                    {
                        mySqlCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {

                    }

                }


            }
            catch (Exception ex)
            {
                // return Ok("dude, I did what you told me to do!!");
            }


            return Ok();
        }

        private string getJson()
        {
            string date = DateTime.Today.ToString("yyyy-MM-dd");
            string gte = string.Empty;
            string lte = string.Empty;




            int currentHour = Convert.ToInt32(DateTime.Now.Hour);

            if (currentHour > 6 && currentHour < 12)
            {
                gte = DateTime.Today.Date.AddHours(0.000000001).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();
                lte = DateTime.Today.Date.AddHours(6).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();
            }
            else if (currentHour > 12 && currentHour < 18)
            {
                gte = DateTime.Today.Date.AddHours(6.000000001).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();
                lte = DateTime.Today.Date.AddHours(12).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();
            }
            else if (currentHour > 18 && currentHour < 24)
            {
                gte = DateTime.Today.Date.AddHours(12.000000001).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();
                lte = DateTime.Today.Date.AddHours(18).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();
            }
            else
            {
                gte = DateTime.Today.Date.AddHours(18.000000001).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();
                lte = DateTime.Today.Date.AddHours(24).Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString();
            }

            var ApiRequestBody = @"{""index"":[""logs-" + date + @"""],""ignore_unavailable"":true}";
            ApiRequestBody = ApiRequestBody + Environment.NewLine + @"{""size"":500,""sort"":[{""log_time"":{""order"":""desc"",""unmapped_type"":""boolean""}}],""query"":{""bool"":{""must"":[{""query_string"":{""query"":""app_name: chatops AND category: call_action AND call_action: *"",""analyze_wildcard"":true}},{""range"":{""log_time"":{""gte"":" + gte + @",""lte"":" + lte + @",""format"":""epoch_millis""}}}],""must_not"":[]}},""highlight"":{""pre_tags"":[""@kibana-highlighted-field@""],""post_tags"":[""@/kibana-highlighted-field@""],""fields"":{""*"":{}},""require_field_match"":false,""fragment_size"":2147483647},""_source"":{""excludes"":[]},""aggs"":{""2"":{""date_histogram"":{""field"":""log_time"",""interval"":""3h"",""time_zone"":""Asia/Kolkata"",""min_doc_count"":1}}},""stored_fields"":[""*""],""script_fields"":{},""docvalue_fields"":[""check_in"",""search_date"",""check_out"",""time_stamp"",""pickup_date"",""dropoff_date"",""log_time"",""@timestamp"",""Timestamp"",""start_time"",""lastrun"",""lastsync""]}" + Environment.NewLine;

            return ApiRequestBody;
        }
    }
}
