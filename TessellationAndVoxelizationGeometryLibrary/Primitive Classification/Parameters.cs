using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TVGL.PrimitiveClassificationDetail
{
    internal static class Parameters
    {

        internal const double Classifier_MinAreaForStartFace = 0.0001;

        internal const double AbnLe1 = 0.25; internal const double AbnLe2 = 0.5;
        internal const double AbnMs1 = 0.25; internal const double AbnMs2 = 0.5;
        internal const double AbnMe1 = 22; internal const double AbnMe2 = 25;
        internal const double AbnHs1 = 22; internal const double AbnHs2 = 25;

        internal const double McmLe1 = 0.01; internal const double McmLe2 = 0.015;
        internal const double McmMs1 = 0.01; internal const double McmMs2 = 0.015;
        internal const double McmMe1 = 0.2; internal const double McmMe2 = 0.3;
        internal const double McmHs1 = 0.2; internal const double McmHs2 = 0.3;

        internal const double SmLe1 = 0.1; internal const double SmLe2 = 0.2;
        internal const double SmMs1 = 0.1; internal const double SmMs2 = 0.2;
        internal const double SmMe1 = 1.8; internal const double SmMe2 = 2;
        internal const double SmHs1 = 1.8; internal const double SmHs2 = 2;
        internal const double MinConeGaussPlaneOffset = 0.1; // sine of 1 degrees 0.1

        internal static List<double> MakingListOfLimABNbeta2()
        {

            var listOfLimits = new List<double>
            {
                AbnLe1,
                AbnLe2,
                AbnMs1,
                AbnMs2,
                AbnMe1,
                AbnMe2,
                AbnHs1,
                AbnHs2,
            };
            return listOfLimits;
        }
        internal static List<double> MakingListOfLimMCMbeta2()
        {
            var listOfLimits = new List<double>
            {
                McmLe1,
                McmLe2,
                McmMs1,
                McmMs2,
                McmMe1,
                McmMe2,
                McmHs1,
                McmHs2,
            };
            return listOfLimits;
        }
        internal static List<double> MakingListOfLimSMbeta2()
        {
            var listOfLimits = new List<double>
            {
                SmLe1,
                SmLe2,
                SmMs1,
                SmMs2,
                SmMe1,
                SmMe2,
                SmHs1,
                SmHs2,
            };
            return listOfLimits;
        }

        internal static List<List<int>> readingEdgesRules2()
        {
            var reader = getStreamReader("NewEdgeRules.csv"); // "EdRulesBeta.csv"
            //var reader = new StreamReader(File.OpenRead("src/PrimitiveClassificationOfTessellatedSolids/NewEdgeRules.csv"));
            var Lists = new List<List<int>>();
            bool blocker = true;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                var i = 0;
                if (blocker)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        var ini = new List<int>();
                        Lists.Add(ini);
                    }
                    blocker = false;
                }
                while (i < 4)
                {
                    Lists[i].Add(Convert.ToInt32(values[i]));
                    i++;
                }
            }
            return Lists;
        }

        internal static List<List<int>> readingFacesRules()
        {
            var reader = getStreamReader("NewFaRules.csv");//FaRules2.csv
            // var reader = new StreamReader(File.OpenRead("src/PrimitiveClassificationOfTessellatedSolids/NewFaRules.csv"));
            List<List<int>> Lists = new List<List<int>>();
            bool blocker = true;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                var i = 0;
                if (blocker)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        var ini = new List<int>();
                        Lists.Add(ini);
                    }
                    blocker = false;
                }
                while (i < 7)
                {
                    Lists[i].Add(Convert.ToInt32(values[i]));
                    i++;
                }
            }

            return Lists;
        }

        private static StreamReader getStreamReader(string filepath)
        {
#if net45
            var a = typeof(Parameters).GetTypeInfo().Assembly;
#else
            var a = Assembly.GetExecutingAssembly();
#endif
            var stream1 = a.GetManifestResourceStream(@"TVGL.Primitive_Classification." + filepath);
            if (stream1 == null)
            {
                var resources = a.GetManifestResourceNames();
                filepath = resources.First(r => r.EndsWith(filepath));
                stream1 = a.GetManifestResourceStream(filepath);
            }
            return new StreamReader(stream1);

        }


    }
}
