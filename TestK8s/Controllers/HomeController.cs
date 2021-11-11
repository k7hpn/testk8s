using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using TestK8s.Models;

namespace TestK8s.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("[action]")]
        public IActionResult Info()
        {
            var now = DateTime.Now;

            var environment = new Dictionary<string, string>
            {
                {"Program Version", Program.GetVersion() },
                {"Machine name", Environment.MachineName },
                {"OS Version", Environment.OSVersion.ToString() },
                {"Processor count", Environment.ProcessorCount.ToString() },
                {"64-bit OS", Environment.Is64BitOperatingSystem.ToString() },
                {"64-bit Process", Environment.Is64BitProcess.ToString() },
                { "System page size", Environment.SystemPageSize.ToString() },
                { "Environment Version", Environment.Version.ToString() },
                { "SessionId", HttpContext.Session.Id },
                { "Current time", now.ToString() },
                { "Current local ip/port", HttpContext.Connection.LocalIpAddress.ToString()
                    + "/"
                    + HttpContext.Connection.LocalPort.ToString() },
                { "Current remote ip/port", HttpContext.Connection.RemoteIpAddress.ToString()
                    + "/"
                    + HttpContext.Connection.RemotePort.ToString() },
            };

            if (HttpContext.Session.TryGetValue("CreatedAt", out byte[] ticksBytes))
            {
                environment.Add("Session created time",
                    DateTime.FromBinary(BitConverter.ToInt64(ticksBytes, 0)).ToString());
            }
            string host = "unknown";
            if (HttpContext.Session.TryGetValue("CreatedLocalIpAddress", out byte[] createdLocalIpBytes))
            {
                host = new IPAddress(createdLocalIpBytes).ToString();
            }
            if (HttpContext.Session.TryGetValue("CreatedLocalPort", out byte[] createdLocalPortBytes))
            {
                environment.Add("Session created local ip/port", host + "/" + BitConverter.ToInt32(createdLocalPortBytes).ToString());
            }
            if (HttpContext.Session.TryGetValue("CreatedRemoteIpAddress", out byte[] createdRemoteIpBytes))
            {
                host = new IPAddress(createdRemoteIpBytes).ToString();
            }
            if (HttpContext.Session.TryGetValue("CreatedRemoteIpPort", out byte[] createdRemoteIp))
            {
                environment.Add("Session created remote ip/port", host + "/" + BitConverter.ToInt32(createdRemoteIp).ToString());
            }

            TempData["environment"] = environment;

            return View();
        }
    }
}