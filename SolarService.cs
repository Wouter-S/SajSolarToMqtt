using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Mqtt;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SajSolarToMqtt
{
    public class SolarService : IHostedService
    {
        private static IMqttClient _mqttClient;
        private HttpClient _solarClient;
        private Settings _settings;
        private Timer _timer;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var environment = Environment.GetEnvironmentVariables();

            _settings = new Settings()
            {
                PublishTopic = (string)environment["MqttPubTopic"],
                MqttIp = (string)environment["MqttIp"],
                MqttPort = int.Parse((string)environment["MqttPort"]),
                InverterEndpoint = (string)environment["InverterEndpoint"],
                DebugMode = bool.Parse((string)environment["DebugMode"]),
            };

            try
            {
                await StartReading();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to start: " + e.Message);
            }
        }

        public async Task StartReading()
        {
            Console.WriteLine($"Connecting to Mqtt: {JsonConvert.SerializeObject(_settings)}");

            _solarClient = new HttpClient()
            {
                BaseAddress = new Uri(_settings.InverterEndpoint)
            };

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(30);

            var configuration = new MqttConfiguration { Port = _settings.MqttPort };

            _mqttClient = await MqttClient.CreateAsync(_settings.MqttIp, _settings.MqttPort);
            await _mqttClient.ConnectAsync();

            Console.WriteLine("Connected to Mqtt broker");

            _timer = new Timer(
                async (e) => await GetReading(),
                null,
                startTimeSpan,
                periodTimeSpan);
        }

        public async Task<string> GetTestReading()
        {
            var xmlString = "<?xml version=\"1.0\" ?> <real_time_data> <state>Normal</state> <Vac_l1>234.5</Vac_l1> <Vac_l2>-</Vac_l2> <Vac_l3>-</Vac_l3> <Iac_l1>0.48</Iac_l1> <Iac_l2>-</Iac_l2> <Iac_l3>-</Iac_l3> <Freq1>49.98</Freq1> <Freq2>-</Freq2> <Freq3>-</Freq3> <pac1>52</pac1> <pac2>-</pac2> <pac3>-</pac3> <p-ac>52</p-ac> <temp>26.5</temp> <e-today>0.95</e-today> <t-today>4.2</t-today> <e-total>2320.56</e-total> <CO2>2313.60</CO2> <t-total>4635.3</t-total> <v-pv1>220.7</v-pv1> <v-pv2>0.0</v-pv2> <v-pv3>-</v-pv3> <v-bus>363.9</v-bus> <maxPower>866</maxPower> <i-pv11>0.24</i-pv11> <i-pv12>-</i-pv12> <i-pv13>-</i-pv13> <i-pv14>-</i-pv14> <i-pv21>-</i-pv21> <i-pv22>-</i-pv22> <i-pv23>-</i-pv23> <i-pv24>-</i-pv24> <i-pv31>-</i-pv31> <i-pv32>-</i-pv32> <i-pv33>-</i-pv33> <i-pv34>-</i-pv34> </real_time_data>";
            return xmlString;
        }

        public async Task GetReading()
        {
            string xmlString;
            if (_settings.DebugMode)
            {
                xmlString = await GetTestReading();
            }
            else
            {
                var response = await _solarClient.GetAsync("real_time_data.xml");
                xmlString = await response.Content.ReadAsStringAsync();
            }
          
            await ParseReading(xmlString);
        }

        private async Task ParseReading(string xmlString)
        {
            SolarReading reading = xmlString.XmlDeserializeFromString<SolarReading>();
            var content = JsonConvert.SerializeObject(reading);
            var message = new MqttApplicationMessage(_settings.PublishTopic, Encoding.UTF8.GetBytes(content));
            await _mqttClient.PublishAsync(message, MqttQualityOfService.AtMostOnce);
            Console.WriteLine($"Sent message: {content}");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public static class XmlExtention
    {
        public static T XmlDeserializeFromString<T>(this string objectData)
        {
            return (T)XmlDeserializeFromString(objectData, typeof(T));
        }

        public static object XmlDeserializeFromString(this string objectData, Type type)
        {
            var serializer = new XmlSerializer(type);
            object result;

            using (TextReader reader = new StringReader(objectData))
            {
                result = serializer.Deserialize(reader);
            }

            return result;
        }
    }
    public struct Settings
    {
        public string PublishTopic;
        public int MqttPort;
        public string MqttIp;
        public bool DebugMode;
        public string InverterEndpoint;
    }
}
