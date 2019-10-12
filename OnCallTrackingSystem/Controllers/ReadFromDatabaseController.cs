using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using OnCallTrackingSystem.Dtos;

namespace OnCallTrackingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReadFromDatabaseController : ControllerBase
    {
        private string _connection;
        public ReadFromDatabaseController()
        {
            _connection = "SERVER=127.0.0.1;PORT=3306;DATABASE=oncalltracking;UID=root;PASSWORD=1596324780";

        }

        [HttpGet(Name = nameof(GetAllEmployee))]
        public ActionResult GetAllEmployee()
        {
            MySqlConnection mySqlConnection = new MySqlConnection(_connection);
            MySqlDataReader mySqlDataReader = null;
            try
            {
                mySqlConnection.Open();

                string SqlCommandString = "select employee.employeeid as Id,FullName,sum(init) as Initiated, sum(ack) as Acknowledged from EmpInitAckData, employee where empinitackdata.employeeid = employee.employeeid group by employee.employeeid order by(ack) desc;";
                MySqlCommand mySqlCommand = new MySqlCommand(SqlCommandString, mySqlConnection);
                mySqlDataReader = mySqlCommand.ExecuteReader();

            }
            catch (Exception)
            {               
            }           

            List<AllEmployees> allEmployees = new List<AllEmployees>();
            while (mySqlDataReader.Read())
            {
                allEmployees.Add(new AllEmployees()
                {
                    Id = mySqlDataReader.GetString(mySqlDataReader.GetOrdinal("Id")),
                    FullName = mySqlDataReader.GetString(mySqlDataReader.GetOrdinal("FullName")),
                    Initiated = mySqlDataReader.GetInt32(mySqlDataReader.GetOrdinal("Initiated")),
                    Acknowledged = mySqlDataReader.GetInt32(mySqlDataReader.GetOrdinal("Acknowledged"))
                });
            }
            mySqlConnection.Close();
            return Ok(allEmployees);
        }
    }
}