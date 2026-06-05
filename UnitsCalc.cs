using System;
using System.Collections.Generic;
using System.Linq;

using System.Text.RegularExpressions;


namespace Virinco.WATS.Interface
{
    public static class UnitsCalc
    {
        public class PrefixUnit
        {
            public string Symbol { get; set; }
            public double UnitFactor { get; set; }
            public double GetValue(double value) { return value * UnitFactor; }
            public double GetValueInv(double value) { return value / UnitFactor; }
        }

        //PrefixUnit
        static List<PrefixUnit> prefixUnit = new List<PrefixUnit>()
        {
            new PrefixUnit() {Symbol="p",UnitFactor=0.000000000001},  //Pico 
            new PrefixUnit() {Symbol="n",UnitFactor=0.000000001},  //Nano
            new PrefixUnit() {Symbol="µ",UnitFactor=0.000001},  //Micro 
            new PrefixUnit() {Symbol="u",UnitFactor=0.000001},  //Micro 
            new PrefixUnit() {Symbol="m",UnitFactor=0.001},  //Milli 
            new PrefixUnit() {Symbol="" ,UnitFactor=1.0},  //No unit prefix
            new PrefixUnit() {Symbol="f" ,UnitFactor=1.0}, //Used for Error in FPT
            new PrefixUnit() {Symbol="k",UnitFactor=1000},  //Kilo
            new PrefixUnit() {Symbol="K",UnitFactor=1000},  //Kilo
            new PrefixUnit() {Symbol="M",UnitFactor=1000000}, //Mega 
            new PrefixUnit() {Symbol="G",UnitFactor=1000000000} //Giga
        };

        //public class Unit
        //{
        //    public string Name { get; set; }
        //    public string Symbol { get; set; }
        //}

        //List<Unit> units = new List<Unit>()
        //{
        //    new Unit() {Name="Ampere (amp)", Symbol="A"},
        //    new Unit() {Name="Volt", Symbol="V"},
        //    new Unit() {Name="Ohm", Symbol="Ω"},
        //    new Unit() {Name="Ohm", Symbol="Ohm"},
        //    new Unit() {Name="Watt", Symbol="W"},
        //    new Unit() {Name="Volt-Ampere", Symbol="VA"},
        //    new Unit() {Name="Farad", Symbol="F"},
        //    new Unit() {Name="Henry", Symbol="H"},
        //    new Unit() {Name="siemens / mho", Symbol="S"},
        //    new Unit() {Name="Coulomb", Symbol="C"},
        //    new Unit() {Name="Ampere-hour", Symbol="Ah"},
        //    new Unit() {Name="Joule", Symbol="J"},
        //    new Unit() {Name="Watt-hour", Symbol="Wh"},
        //    new Unit() {Name="Electron-volt", Symbol="eV"},
        //    new Unit() {Name="Tesla", Symbol="T"},
        //    new Unit() {Name="Gauss", Symbol="G"},
        //    new Unit() {Name="Weber", Symbol="Wb"},
        //    new Unit() {Name="Hertz", Symbol="Hz"},
        //    new Unit() {Name="Seconds", Symbol="s"},
        //    new Unit() {Name="Meter / metre", Symbol="m"},
        //    new Unit() {Name="Square-meter", Symbol="m2"},
        //    new Unit() {Name="Decibel", Symbol="dB"}
        //};

        /// <summary>
        /// Converts value from unit to another unit using prefixes pnµmKMG
        /// </summary>
        /// <param name="fromUnit">Unit for fromValue</param>
        /// <param name="fromValue">From value</param>
        /// <param name="toUnit">Wanted unit (Just prefix is used)</param>
        /// <returns></returns>
        public static double AlignUnits(string fromUnit, double fromValue, string toUnit)
        {

            Regex r = new Regex(@"(?<Prefix>[fpnµumkKMG]*)(?<Unit>.+)");
            Match m = r.Match(fromUnit);
            string fromPrefix = m.Groups["Prefix"].Value;
            string fromUnitBare = m.Groups["Unit"].Value;
            double realValue = fromValue;
            if (!String.IsNullOrEmpty(fromPrefix))
                realValue = prefixUnit.Where(p => p.Symbol == fromPrefix).First().GetValue(fromValue);
            m = r.Match(toUnit);
            string toPrefix = m.Groups["Prefix"].Value;
            string toUnitBare = m.Groups["Unit"].Value;
            if (fromUnitBare != toUnitBare)
                throw new ApplicationException($"From unit {fromUnit} and to unit {toUnit} can only differ by prefix");
            double toValue = realValue;
            if (!String.IsNullOrEmpty(toPrefix))
                toValue = prefixUnit.Where(p => p.Symbol == toPrefix).First().GetValueInv(realValue);
            return toValue;
        }
    }
}
