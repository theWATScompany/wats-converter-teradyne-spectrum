using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Virinco.WATS.Integration.TextConverter;
using Virinco.WATS.Interface;

namespace Virinco.WATS.Converter.Teradyne
{
    class MainStep
    {
        public string Name { get; set; }
        public StepStatusType Status { get; set; }
        public DateTime Start { get; set; }
        public string Description { get; set; }
        public List<SubStep> SubSteps { get; set; }
    }

    class SubStep
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double? Meas { get; set; }
        public string MeasScale { get; set; }
        public string Unit { get; set; }
        public double? LowLim { get; set; }
        public string LowScale { get; set; }
        public double? HighLim { get; set; }
        public string HighScale { get; set; }
        public string Comment { get; set; }
        public StepStatusType Status { get; set; }
    }

    public class TerradyneSpectrumICTConverter : TextConverterBase
    {
        public TerradyneSpectrumICTConverter() : base()
        {
            ConverterParameters["operationTypeCode"] = "30";
        }

        string previousLines = "";
        protected override string PreProcessLine(string line)
        {
            line= line.Trim();
            if (!string.IsNullOrEmpty(line) && !(line.EndsWith(":") || line.EndsWith(")"))) //Handle Comments etc. with newline
            {
                previousLines+=line;
                return "";
            }
            else if (!string.IsNullOrEmpty(previousLines))
            {
                string combinedLine = previousLines + line;
                previousLines = "";
                return base.PreProcessLine(combinedLine);
            }
            return base.PreProcessLine(line);
        }

        MainStep currentMainStep = null;
        MainStep prevMainStep = null;

        protected override bool ProcessMatchedLine(SearchFields.SearchMatch match, ref ReportReadState readState)
        {
            if (match == null)
            {
                currentUUT.ExecutionTime = (currentMainStep.Start - currentUUT.StartDateTime).TotalSeconds;
                SubmitUUT();
                return true;
            }
            Console.WriteLine(match.completeLine);
            switch (match.matchField.fieldName)
            {
                case "ProgramName":
                    apiRef.TestMode = TestModeType.Import;
                    currentMainStep = null;
                    currentUUT.PartNumber = (string)match.GetSubField("ProgramName");
                    currentUUT.AddMiscUUTInfo("File", apiRef.ConversionSource.SourceFile.Name);
                    break;
                case "NewSequence":
                    if (currentMainStep != null)
                    {
                        CreateSteps(currentMainStep, currentSequence, prevMainStep); //Create UUT steps for previous step
                        if (currentMainStep != null && currentMainStep.Start != DateTime.MinValue)
                            prevMainStep = currentMainStep;
                        currentMainStep = new MainStep();
                        currentMainStep.SubSteps = new List<SubStep>();
                    }
                    currentSequence = currentUUT.GetRootSequenceCall().AddSequenceCall((string)match.GetSubField("SequenceName"));

                    break;
                case "NewStep":
                    if (currentMainStep == null) //First step in file, use for UUTStart
                        currentUUT.StartDateTime = (DateTime)match.GetSubField("TIME");
                    else
                    {
                        CreateSteps(currentMainStep, currentSequence, prevMainStep); //Create UUT steps for previous step
                    }
                    if (currentMainStep != null && currentMainStep.Start != DateTime.MinValue)
                        prevMainStep = currentMainStep;
                    currentMainStep = new MainStep() { Start = (DateTime)match.GetSubField("TIME") };
                    currentMainStep.SubSteps = new List<SubStep>();
                    break;
                case "SNAME":
                    currentMainStep.Name = (string)match.GetSubField("SNAME");
                    break;
                case "Shorts":
                    currentMainStep.SubSteps.Last().Comment += ((string)match.GetSubField("COMM1"), (string)match.GetSubField("COMM2"));
                    break;
                case "DESC":
                    currentMainStep.Description = (string)match.GetSubField("DESC");
                    break;
                case "StepType":
                    currentMainStep.SubSteps.Add(new SubStep() { Name = (string)match.GetSubField("PAGE_NAME"), Comment = (string)match.GetSubField("COMMENT"), Status = (StepStatusType)match.GetSubField("STAT") });
                    break;
                case "ConnectedNodes":
                    currentMainStep.SubSteps.Last().Name+=$" - {(string)match.GetSubField("TPNAME")}";
                    break;
                case "GROUP_NAME":
                    currentMainStep.SubSteps.Add(new SubStep() { Name = (string)match.GetSubField("GROUP_NAME"), Meas = (double)match.GetSubField("VAL"), Status=(StepStatusType)match.GetSubField("STAT"),HighLim=currentMainStep.SubSteps[0].HighLim });
                    break;
                case "FSCAN_PIN":
                    currentMainStep.SubSteps.Add(new SubStep() { Name = (string)match.GetSubField("TPNAME"), Status = (StepStatusType)match.GetSubField("STAT")});
                    break;
                case "MEAS_VAL_THRESHOLD":
                    currentMainStep.SubSteps.Last().Meas = (double)match.GetSubField("VAL");
                    currentMainStep.SubSteps.Last().LowLim = (double)match.GetSubField("THRESH");
                    break;
                case "LOLIM":
                    currentMainStep.SubSteps.Last().LowLim = (double)match.GetSubField("VAL");
                    currentMainStep.SubSteps.Last().LowScale = (string)match.GetSubField("SCALE");
                    break;
                case "HILIM":
                    currentMainStep.SubSteps.Last().HighLim = (double)match.GetSubField("VAL");
                    currentMainStep.SubSteps.Last().HighScale = (string)match.GetSubField("SCALE");
                    break;
                case "MEASVAL":
                    currentMainStep.SubSteps.Last().Meas = (double)match.GetSubField("VAL");
                    currentMainStep.SubSteps.Last().MeasScale = (string)match.GetSubField("SCALE");
                    currentMainStep.SubSteps.Last().Unit = (string)match.GetSubField("UNIT");
                    break;
                case "OrderNumber":
                    currentUUT.AddMiscUUTInfo("OrderNumber", (string)match.GetSubField("Val"));
                    break;
                case "CustomerSerialnumber":
                    currentUUT.AddMiscUUTInfo("CustomerSN", (string)match.GetSubField("Val"));
                    break;
                case "ProgName":
                    currentMainStep.SubSteps.Last().Comment = $"EXT_PROG: {match.GetSubField("PName")}";
                    break;

                default: break;
            }
            return true;
        }


        Step stepToHaveTime = null;
        private void CreateSteps(MainStep tmpMainStep, SequenceCall seq, MainStep prevStep)
        {
            if (stepToHaveTime != null && tmpMainStep.Start != DateTime.MinValue && prevStep != null && prevStep.Start != DateTime.MinValue)
                stepToHaveTime.StepTime = (tmpMainStep.Start - prevMainStep.Start).TotalSeconds;

            if (tmpMainStep.SubSteps.Count > 1)
            {
                seq = seq.AddSequenceCall(tmpMainStep.Name);
                if (!string.IsNullOrEmpty(tmpMainStep.Description))
                    seq.ReportText = tmpMainStep.Description;
                stepToHaveTime = seq;
            }
            Step lastStep = null; ;
            foreach (SubStep subStep in tmpMainStep.SubSteps)
            {
                if (subStep.Type == "DELAY")
                    currentStep = seq.AddGenericStep(GenericStepTypes.Wait, subStep.Name);
                else
                if (subStep.Meas != null)
                {
                    NumericLimitStep numericLimitStep = seq.AddNumericLimitStep(subStep.Name);
                    CheckForUnitAndScale(subStep);
                    if (subStep.LowLim != null && subStep.HighLim != null)
                        numericLimitStep.AddTest((double)subStep.Meas, CompOperatorType.GELE, (double)subStep.LowLim, (double)subStep.HighLim, subStep.MeasScale + subStep.Unit);
                    else if (subStep.Meas != null && subStep.LowLim != null)
                    {
                        numericLimitStep.AddTest((double)subStep.Meas, CompOperatorType.GE, (double)subStep.LowLim, subStep.MeasScale + subStep.Unit);
                    }
                    else if (subStep.Meas != null && subStep.HighLim != null)
                    {
                        numericLimitStep.AddTest((double)subStep.Meas, CompOperatorType.LE, (double)subStep.HighLim, subStep.MeasScale + subStep.Unit);
                    }
                    else
                    {
                        numericLimitStep.AddTest((double)subStep.Meas, subStep.Unit);
                    }
                    currentStep = numericLimitStep;
                }
                else
                {
                    currentStep = seq.AddGenericStep(GenericStepTypes.Action, subStep.Name);
                }
                if (subStep.Comment != null)
                {
                    string comment = subStep.Comment.Replace("Analysis Comments:", "").Replace("Testable.", "");
                    if (!string.IsNullOrEmpty(comment))
                        currentStep.ReportText = comment;
                }
                currentStep.Status = subStep.Status;
                if (subStep.Status == StepStatusType.Failed && currentStep.Parent != null)
                {
                    //Max two levels up to root
                    currentStep.Parent.Status = StepStatusType.Failed;
                    if (currentStep.Parent.Parent!=null)
                        currentStep.Parent.Parent.Status= StepStatusType.Failed;
                }
                lastStep = currentStep;
            }
            if (tmpMainStep.SubSteps.Count == 1)
            {
                if (!string.IsNullOrEmpty(tmpMainStep.Description))
                    lastStep.ReportText = $"{tmpMainStep.Description} {lastStep.ReportText}";
                stepToHaveTime = lastStep;
            }
        }

        private void CheckForUnitAndScale(SubStep subStep)
        {
            if (string.IsNullOrEmpty(subStep.Unit)) subStep.Unit = "?";
            if (subStep.LowLim != null && subStep.LowScale != subStep.MeasScale)
                subStep.LowLim = UnitsCalc.AlignUnits(subStep.LowScale + subStep.Unit, (double)subStep.LowLim, subStep.MeasScale + subStep.Unit);
            if (subStep.HighLim != null && subStep.HighScale != subStep.MeasScale)
                subStep.HighLim = UnitsCalc.AlignUnits(subStep.HighScale + subStep.Unit, (double)subStep.HighLim, subStep.MeasScale + subStep.Unit);
        }

        public TerradyneSpectrumICTConverter(IDictionary<string, string> args)
        : base(args)
        {

            SearchFields.RegExpSearchField fmt = searchFields.AddRegExpField("ProgramName", ReportReadState.InHeader, @"\x28PROGRAM_NAME: ""(?<ProgramName>.+)""\x29", null, typeof(string), ReportReadState.InTest);
            fmt.AddSubField("ProgramName", typeof(string));

            fmt = searchFields.AddRegExpField("NewSequence", ReportReadState.InTest, @"\x28SECTION_NAME: ""(?<SequenceName>.+)""\x29", null, typeof(string));
            fmt.AddSubField("SequenceName", typeof(string));

            fmt = searchFields.AddRegExpField("NewStep", ReportReadState.InTest, @"\x28TYPE: (?<TYPE>.+)\x29 \x28TIME: (?<TIME>.+?) *\x29", "", typeof(string));
            fmt.AddSubField("TYPE", typeof(string));
            fmt.AddSubField("TIME", typeof(DateTime), "MM/dd/yyyy HH:mm:ss"); //09/27/2023 10:12:57

            fmt = searchFields.AddRegExpField("SNAME", ReportReadState.InTest, @"\x28SNAME: ""(?<SNAME>.+)""\x29", "", typeof(string));
            fmt.AddSubField("SNAME", typeof(string), null);

            fmt = searchFields.AddRegExpField("DESC", ReportReadState.InTest, @"\x28DESC: ""(?<DESC>.+?) *""\x29 \x28PAGE:", "", typeof(string));
            fmt.AddSubField("DESC", typeof(string));

            fmt = searchFields.AddRegExpField("StepType", ReportReadState.InTest, @"(\x28TYPE: (?<TYPE>.+)\x29)* *\x28PAGE_NAME: ""(?<PAGE_NAME>.+?)""\x29 (\x28COMMENT: ""(?<COMMENT>.+?)""\x29)* *\x28STAT: (?<STAT>.+)\x29", "", typeof(string));
            fmt.AddSubField("TYPE", typeof(string));
            fmt.AddSubField("PAGE_NAME", typeof(string));
            fmt.AddSubField("COMMENT", typeof(string));
            fmt.AddSubField("STAT", typeof(StepStatusType));

            fmt = searchFields.AddRegExpField("GROUP_NAME", ReportReadState.InTest, @".*\(GROUP_NAME: (?<GROUP_NAME>[\w\d]+)\) \(STAT: (?<STAT>[\w]+)\)\(MEASVAL: \(VAL: (?<VAL>[0-9.]+)\) \(UNIT: (?<UNIT>\w+)\) \)", null, typeof(string));
            fmt.AddSubField("GROUP_NAME", typeof(string));
            fmt.AddSubField("STAT", typeof(StepStatusType));
            fmt.AddSubField("VAL", typeof(double));
            fmt.AddSubField("UNIT", typeof(string));

            fmt = searchFields.AddRegExpField("ConnectedNodes", ReportReadState.InTest, @"\s*\x28CONNECTED_NODES:.*?TPNAME: (?<TPNAME>.+?)\x29.*", "", typeof(string));
            fmt.AddSubField("TPNAME", typeof(string));

            fmt = searchFields.AddRegExpField("Shorts", ReportReadState.InTest, @"\x28(?<COMM1>SC_FROM_NODES:.+)|\x28(?<COMM2>SC_TO_NODES:.+)", "", typeof(string));
            fmt.AddSubField("COMM1", typeof(string));
            fmt.AddSubField("COMM2", typeof(string));

            fmt = searchFields.AddRegExpField("FSCAN_PIN", ReportReadState.InTest, @".*\(FSCAN_PIN: \(STAT: (?<STAT>[\w]+)\).* \(TPNAME: (?<TPNAME>\w+)\) \)", null, typeof(string));
            fmt.AddSubField("STAT", typeof(StepStatusType));
            fmt.AddSubField("TPNAME", typeof(string));

            fmt = searchFields.AddRegExpField("MEAS_VAL_THRESHOLD", ReportReadState.InTest, @"\(MEAS_VAL: \(VAL: (?<VAL>[0-9+-.E]+)\) \) \(THRESH: (?<THRESH>[0-9+-.E]+)\)", null, typeof(string));
            fmt.AddSubField("VAL", typeof(double));
            fmt.AddSubField("THRESH", typeof(double));

            searchFields.AddRegExpField(UUTField.SerialNumber, ReportReadState.InTest, @"\x28USER: *Serialnumber: *(?<Val>.+?)\x29", "", typeof(string));
            searchFields.AddRegExpField(UUTField.Operator, ReportReadState.InTest, @"\x28USER: *Operator: *(?<SN>.+?)\x29", "", typeof(string));

            fmt = searchFields.AddRegExpField("OrderNumber", ReportReadState.InTest, @"\x28USER: *OrderNumber: *(?<Val>.+?)\x29", "", typeof(string));
            fmt.AddSubField("Val", typeof(string));
            fmt = searchFields.AddRegExpField("CustomerSerialnumber", ReportReadState.InTest, @"\x28USER: *CustomerSerialnumber: *(?<Val>.+?)\x29", "", typeof(string));
            fmt.AddSubField("Val", typeof(string));

            string measTemplate = @"\x28{Meas}: *\(VAL: (?<VAL>[0-9+-.E]+?)\) *(\(\SCALE: (?<SCALE>.+?)\))* (\(UNIT: (?<UNIT>.+?)\))* *\)";

            fmt = searchFields.AddRegExpField("LOLIM", ReportReadState.InTest, measTemplate.Replace("{Meas}", "LOLIM"), "", typeof(string));
            fmt.AddSubField("VAL", typeof(double), null);
            fmt.AddSubField("SCALE", typeof(string), null);
            fmt.AddSubField("UNIT", typeof(string), null);

            fmt = searchFields.AddRegExpField("LOLIM", ReportReadState.InTest, measTemplate.Replace("{Meas}", "LOW"), "", typeof(string));
            fmt.AddSubField("VAL", typeof(double), null);
            fmt.AddSubField("SCALE", typeof(string), null);
            fmt.AddSubField("UNIT", typeof(string), null);


            fmt = searchFields.AddRegExpField("HILIM", ReportReadState.InTest, measTemplate.Replace("{Meas}", "HILIM"), "", typeof(string));
            fmt.AddSubField("VAL", typeof(double), null);
            fmt.AddSubField("SCALE", typeof(string), null);
            fmt.AddSubField("UNIT", typeof(string), null);

            fmt = searchFields.AddRegExpField("HILIM", ReportReadState.InTest, measTemplate.Replace("{Meas}", "HIGH"), "", typeof(string));
            fmt.AddSubField("VAL", typeof(double), null);
            fmt.AddSubField("SCALE", typeof(string), null);
            fmt.AddSubField("UNIT", typeof(string), null);

            fmt = searchFields.AddRegExpField("MEASVAL", ReportReadState.InTest, measTemplate.Replace("{Meas}", "MEASVAL"), "", typeof(string));
            fmt.AddSubField("VAL", typeof(double), null);
            fmt.AddSubField("SCALE", typeof(string), null);
            fmt.AddSubField("UNIT", typeof(string), null);

            fmt = searchFields.AddRegExpField("ProgName", ReportReadState.InTest, @"\x28PROG_NAME: *(?<PName>.+?)\x29", "", typeof(string));
            fmt.AddSubField("PName", typeof(string), null);


            searchFields.AddRegExpField(UUTField.Status, ReportReadState.InTest, @"\x28USER: *STATUS: BOARD (?<STAT>.+)\x29", "", typeof(UUTStatusType));
        }

    }
}
