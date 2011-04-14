using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ReactiveUI;

namespace RescueTimeByForce
{
    public class ActivityRecord
    {
        public DateTime Time { get; protected set; }
        public TimeSpan Duration { get; protected set; }
        public int ProductivityScore { get; protected set; }

        public double ProductivityInSeconds {
            get { return Duration.TotalSeconds * (ProductivityScore / 1.0); }
        }

        public ActivityRecord(DateTime time, TimeSpan duration, int productivityScore)
        {
            Time = time;
            Duration = duration;
            ProductivityScore = productivityScore;
        }

        public static ActivityRecord ParseRecord(string record)
        {
            string[] fields = record.Split(',');
            DateTime dt;
            int seconds;
            int ps;

            if (!DateTime.TryParse(fields[0], out dt)) {
                return null;
            }

            if (!Int32.TryParse(fields[1], out seconds)) {
                return null;
            }

            if (!Int32.TryParse(fields[5], out ps)) {
                return null;
            }

            return new ActivityRecord(dt, TimeSpan.FromSeconds(seconds), ps);
        }

        public static IEnumerable<string> splitLinesInBuffer(string buffer)
        {
            int start = 0;
            for(int i=0; i < buffer.Length; i++) {
                if (buffer[i] != '\n') {
                    continue;
                }

                int len = i - start;
                if (len > 0) {
                    yield return buffer.Substring(start, len);
                }

                start = i + 1;
            }

            if (buffer.Length - start > 1) {
                yield return buffer.Substring(start);
            }
        }

        public static ActivityRecord[] ParseRescueTimeCSV(Stream data)
        {
            var ms = new MemoryStream(); data.CopyTo(ms);
            var csv = Encoding.UTF8.GetString(ms.GetBuffer());

            return splitLinesInBuffer(csv)
                .Skip(1)
                .Select(ParseRecord)
                .Where(x => x != null)
                .ToArray();
        }

        public static IObservable<ActivityRecord[]> FetchRescueTimeData(string apiKey)
        {
            var now = DateTime.Now;
            string url = String.Format("https://www.rescuetime.com/anapi/data?key={0}&format=csv&pv=interval&rb={1}-{2:00}-{3:00}&re={1}-{2:00}-{3:00}&rs=minute",
                apiKey, now.Year, now.Month, now.Day);

            var hwr = WebRequest.Create(url) as HttpWebRequest;
            var fetchFunc = Observable.FromAsyncPattern<WebResponse>(hwr.BeginGetResponse, hwr.EndGetResponse);

            return fetchFunc().Select(x => ParseRescueTimeCSV(x.GetResponseStream()));
        }
    }

    public static class DoubleMixin
    {
        public static double Clamp(this double This, double? min = null, double? max = null)
        {
            min = min ?? Double.MinValue;
            max = max ?? Double.MaxValue;

            double ret = This > max.Value ? max.Value : This;
            return (ret < min.Value ? min.Value : ret);
        }
    }
}