/*
 | Version 10.1.1
 | Copyright 2012 Esri
 |
 | Licensed under the Apache License, Version 2.0 (the "License");
 | you may not use this file except in compliance with the License.
 | You may obtain a copy of the License at
 |
 |    http://www.apache.org/licenses/LICENSE-2.0
 |
 | Unless required by applicable law or agreed to in writing, software
 | distributed under the License is distributed on an "AS IS" BASIS,
 | WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 | See the License for the specific language governing permissions and
 | limitations under the License.
 */


using System;
using System.Collections.Generic;
using System.Text;
using Esri_Telecom_Tools.Core.Wrappers;
using Esri_Telecom_Tools.Helpers;

namespace Esri_Telecom_Tools.Core.Utils
{
    /// <summary>
    /// Helper methods surrounding connection and splice ranges
    /// </summary>
    public static class SpliceAndConnectionUtils
    {
        /// <summary>
        /// Determines if all given ranges are available (neither spliced nor connected)
        /// </summary>
        /// <param name="units">Units to check</param>
        /// <param name="cableWrapper">Cable to check</param>
        /// <param name="isFromEnd">Flag of which end we are checking</param>
        /// <returns>True if they are all available</returns>
        public static bool AreRangesAvailable(ICollection<int> units, FiberCableWrapper cableWrapper, bool isFromEnd)
        {
            bool result = true;

            List<int> splicedStrands = GetSplicedStrands(cableWrapper, isFromEnd);
            List<int> connectedStrands = GetConnectedStrands(cableWrapper, isFromEnd);

            foreach (int unit in units)
            {
                if (splicedStrands.Contains(unit)
                    || connectedStrands.Contains(unit))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if all given ranges are available (neither spliced nor connected)
        /// </summary>
        /// <param name="units">Units to check</param>
        /// <param name="cableWrapper">Device to check</param>
        /// <param name="isFromEnd">Flag of which port type we are checking</param>
        /// <returns>True if they are all available</returns>
        public static bool AreRangesAvailable(ICollection<int> units, DeviceWrapper deviceWrapper, PortType portType)
        {
            bool result = true;

            List<int> openPorts = GetOpenPorts(deviceWrapper, portType);

            foreach(int unit in units)
            {
                if (!openPorts.Contains(unit))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if a device has any connections on its ports
        /// </summary>
        /// <param name="deviceWrapper">Device to check</param>
        /// <returns>True if any port is connected</returns>
        public static bool HasAnyConnections(DeviceWrapper deviceWrapper)
        {
//            int? inputPortCount = GdbUtils.GetDomainedIntName(deviceWrapper.Feature, ConfigUtil.InputPortsFieldName);
//            int? outputPortCount = GdbUtils.GetDomainedIntName(deviceWrapper.Feature, ConfigUtil.OutputPortsFieldName);

            int? inputPortCount = deviceWrapper.inputPorts;
            int? outputPortCount = deviceWrapper.outputPorts;

            List<Range> ranges = null;

            bool isInputClear = true;
            if (null != inputPortCount && 0 < inputPortCount)
            {
                ranges = GetAvailableRanges(deviceWrapper, PortType.Input);
                isInputClear = AreRangesWholeUnitCount(ranges, (int)inputPortCount);
            }

            bool isOutputClear = true;
            if (null != outputPortCount && 0 < outputPortCount)
            {
                ranges = GetAvailableRanges(deviceWrapper, PortType.Output);
                isOutputClear = AreRangesWholeUnitCount(ranges, (int)outputPortCount);
            }

            // If either port type is not clear, there are connections
            return (!isInputClear || !isOutputClear);
        }

        /// <summary>
        /// Determines if a cable has any connections or splices on its strands
        /// </summary>
        /// <param name="cableWrapper">Cable to check</param>
        /// <returns>True if any strand is connected or spliced</returns>
        public static bool HasAnyConnections(FiberCableWrapper cableWrapper)
        {
            int? fiberCount = cableWrapper.fibers;

            List<Range> ranges = null;

            bool isFromClear = true;
            bool isToClear = true;
            if (null != fiberCount && 0 < fiberCount)
            {
                ranges = GetAvailableRanges(cableWrapper, true);
                isFromClear = AreRangesWholeUnitCount(ranges, (int)fiberCount);

                ranges = GetAvailableRanges(cableWrapper, false);
                isToClear = AreRangesWholeUnitCount(ranges, (int)fiberCount);
            }

            // If either end is not clear, there are connections
            return (!isFromClear || !isToClear);
        }

        /// <summary>
        /// Checks a list of ranges to see if it represents the whole range of units
        /// </summary>
        /// <param name="ranges">List of Range</param>
        /// <param name="unitCount">Total count of fibers or ports</param>
        /// <returns>True if the range is the entire unit count</returns>
        public static bool AreRangesWholeUnitCount(List<Range> ranges, int unitCount)
        {
            // There should only be one Range with low = 1 and high = unitCount
            bool isClear = true;

            if (1 != ranges.Count)
            {
                isClear = false;
            }
            else
            {
                Range range = ranges[0];
                if (1 != range.Low
                    || unitCount != range.High)
                {
                    isClear = false;
                }
            }

            return isClear;
        }

        /// <summary>
        /// Creates ranges for the integer list. Does not sort the list -- if it is not presorted, the ranges may be broken up
        /// more than is necessary
        /// </summary>
        /// <param name="sortedList">List of sorted ints</param>
        /// <returns>List of Range</returns>
        public static List<Range> MergeRanges(List<int> intList)
        {
            List<Range> result = new List<Range>();
            if (null == intList)
            {
                throw new ArgumentNullException("intList");
            }

            int low = -1;
            int prev = -1;

            for (int i = 0; i < intList.Count; i++)
            {
                int current = intList[i];
                if (current != (prev + 1))
                {
                    if (-1 != low)
                    {
                        result.Add(new Range(low, prev));
                    }

                    low = current;
                }

                prev = current;
            }

            if (-1 != low)
            {
                result.Add(new Range(low, prev));
            }

            return result;
        }

        /// <summary>
        /// Takes a range like {1-10, 14, 25} and another like {1-12} and makes them into splice ranges like {1-10} {1-10}, 
        /// {14-14} {11-11}, {25-25} {12-12}
        /// </summary>
        /// <param name="aRanges">A list of Ranges</param>
        /// <param name="bRanges">Another List of Ranges</param>
        /// <returns>List of Connection</returns>
        public static List<Connection> MatchUp(List<Range> aRanges, List<Range> bRanges)
        {
            List<Connection> result = new List<Connection>();

            if (null == aRanges)
            {
                throw new ArgumentNullException("aRanges");
            }

            if (null == bRanges)
            {
                throw new ArgumentNullException("bRanges");
            }

            if (0 < aRanges.Count && 0 < bRanges.Count)
            {
                int currentBRangeIdx = 0;
                Range currentBRange = bRanges[currentBRangeIdx];
                int currentBUnit = currentBRange.Low;

                for (int i = 0; i < aRanges.Count; i++)
                {
                    Range aRange = aRanges[i];
                    int remainingAUnits = aRange.High - aRange.Low + 1;
                    int currentAUnit = aRange.Low;

                    while (0 < remainingAUnits)
                    {
                        int remainingBUnits = currentBRange.High - currentBUnit + 1;

                        if (remainingBUnits >= remainingAUnits)
                        {
                            // We have enough in the B Range to take care of the entire remaining A Range.
                            result.Add(new Connection(new Range(currentAUnit, currentAUnit + remainingAUnits - 1), new Range(currentBUnit, currentBUnit + remainingAUnits - 1)));

                            // Decrement A units left to process; increment our position in the A and B ranges
                            currentBUnit += remainingAUnits;
                            currentAUnit += remainingAUnits;
                            remainingAUnits = 0;
                        }
                        else
                        {
                            // We will use as much of this bRange as we can but will be stuck on this aRange afterwards
                            result.Add(new Connection(new Range(currentAUnit, currentAUnit + remainingBUnits - 1), new Range(currentBUnit, currentBUnit + remainingBUnits - 1)));

                            // Decrement A units left to process; increment our position in the A and B ranges
                            currentBUnit += remainingBUnits;
                            currentAUnit += remainingBUnits;
                            remainingAUnits -= remainingBUnits;
                        }

                        if (currentBUnit > currentBRange.High)
                        {
                            currentBRangeIdx++;

                            if (currentBRangeIdx < bRanges.Count)
                            {
                                currentBRange = bRanges[currentBRangeIdx];
                                currentBUnit = currentBRange.Low;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if the count of units included in one list of ranges matches the count of units in the ohter
        /// </summary>
        /// <param name="a">A range</param>
        /// <param name="b">Another range</param>
        /// <returns>True if unit counts are equal</returns>
        public static bool AreCountsEqual(List<Range> a, List<Range> b)
        {
            int aCount = 0;
            int bCount = 0;

            if (null == a)
            {
                throw new ArgumentNullException("a");
            }

            if (null == b)
            {
                throw new ArgumentNullException("b");
            }

            foreach (Range r in a)
            {
                aCount += (r.High - r.Low + 1);
            }

            foreach (Range r in b)
            {
                bCount += (r.High - r.Low + 1);
            }

            return (aCount == bCount);
        }

        /// <summary>
        /// Returns a list of parsed ranges
        /// </summary>
        /// <param name="rangeString">Format nn-nn{,nn-nn,nn-nn...}</param>
        /// <returns>List of Range</returns>
        public static List<Range> ParseRanges(string rangeString)
        {
            List<Range> result = new List<Range>();
            string[] ranges = null;

            rangeString.Trim();
            rangeString = rangeString.Replace("- ", "-");
            rangeString = rangeString.Replace(" -", "-");
            rangeString = rangeString.Replace(", ", ",");
            rangeString = rangeString.Replace(" ,", ",");

            rangeString = rangeString.Replace(" ", ","); // Treat remaining white space as breaks between ranges
            rangeString = rangeString.Replace(";", ","); // Treat semicolons as breaks between ranges

            if (rangeString.Contains(","))
            {
                ranges = rangeString.Split(',');
            }
            else
            {
                ranges = new string[] { rangeString };
            }

            for (int i = 0; i < ranges.Length; i++)
            {
                string temp = ranges[i];

                if (temp.Contains("-"))
                {
                    string[] splits = temp.Split('-');
                    if (2 == splits.Length)
                    {
                        int low = -1;
                        int high = -1;
                        if (Int32.TryParse(splits[0], out low)
                            && Int32.TryParse(splits[1], out high))
                        {
                            if (0 < low && 0 < high)
                            {
                                result.Add(new Range(low, high));
                            }
                            else
                            {
                                throw new FormatException(string.Format("Units must be positive: {0}", temp));
                            }
                        }
                        else
                        {
                            throw new FormatException(string.Format("Could not parse {0} or {1}", splits[0], splits[1]));
                        }
                    }
                    else
                    {
                        throw new FormatException(string.Format("Invalid range {0}", temp));
                    }
                }
                else
                {
                    int unit = -1;
                    if (Int32.TryParse(temp, out unit))
                    {
                        result.Add(new Range(unit, unit));
                    }
                    else
                    {
                        throw new FormatException(string.Format("Could not parse {0}", temp));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if all the ranges fall between 1 to fibercount
        /// </summary>
        /// <param name="ranges">Ranges to check</param>
        /// <param name="cable">Cable to check on</param>
        /// <returns>True if valid ranges</returns>
        public static bool AreRangesWithinFiberCount(List<Range> ranges, FiberCableWrapper cable)
        {
            bool result = true;

            #region Validation
            if (null == cable)
            {
                throw new ArgumentNullException("cable");
            }

            if (null == ranges)
            {
                throw new ArgumentNullException("ranges");
            }
            #endregion

//            int? fiberCount = GdbUtils.GetDomainedIntName(cable.Feature, ConfigUtil.NumberOfFibersFieldName);

            int? fiberCount = cable.fibers;

            if (null == fiberCount)
            {
                result = false;
            }
            else
            {
                foreach (Range r in ranges)
                {
                    if (r.High > fiberCount
                        || 1 > r.Low)
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if all the ranges fall between 1 to port count
        /// </summary>
        /// <param name="ranges">Ranges to check</param>
        /// <param name="device">Device to check on</param>
        /// <param name="portType">Port type</param>
        /// <returns>True if valid ranges</returns>
        public static bool AreRangesWithinPortCount(List<Range> ranges, DeviceWrapper device, PortType portType)
        {
            bool result = false; // Default to false in case we can't even find the port relationship class

            #region Validation

            if (null == ranges)
            {
                throw new ArgumentNullException("ranges");
            }

            if (null == device)
            {
                throw new ArgumentNullException("device");
            }

            #endregion

            ESRI.ArcGIS.Geodatabase.IFeatureClass ftClass = device.Feature.Class as ESRI.ArcGIS.Geodatabase.IFeatureClass;
            if (null != ftClass)
            {
                using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
                {
                    ESRI.ArcGIS.Geodatabase.IRelationshipClass deviceHasPorts = ConfigUtil.GetPortRelationship(ftClass);
                    if (null != deviceHasPorts)
                    {
                        ESRI.ArcGIS.Geodatabase.ITable portTable = deviceHasPorts.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
                        int portIdIdx = portTable.FindField(ConfigUtil.PortIdFieldName);

                        if (-1 < portIdIdx)
                        {
                            result = true; // Now that we have the ports, assume we're ok until we find a problem

                            ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                            releaser.ManageLifetime(filter);

                            filter.SubFields = ConfigUtil.PortIdFieldName;
                            filter.WhereClause = string.Format("{0}='{1}' AND {2}='{3}' AND {4} IS NOT NULL", 
                                deviceHasPorts.OriginForeignKey, 
                                device.Feature.get_Value(ftClass.FindField(deviceHasPorts.OriginPrimaryKey)),
                                ConfigUtil.PortTypeFieldName,
                                PortType.Input == portType ? 1 : 2,
                                ConfigUtil.PortIdFieldName);

                            ((ESRI.ArcGIS.Geodatabase.IQueryFilterDefinition)filter).PostfixClause = string.Format("ORDER BY {0}", ConfigUtil.PortIdFieldName);
                            ESRI.ArcGIS.Geodatabase.ICursor cursor = portTable.Search(filter, true);
                            releaser.ManageLifetime(cursor);

                            int minPort = int.MinValue;
                            int maxPort = int.MaxValue;
                            ESRI.ArcGIS.Geodatabase.IRow row = cursor.NextRow();
                            
                            if (null != row)
                            {
                                minPort = (int)row.get_Value(portIdIdx);

                                while (null != row)
                                {
                                    maxPort = (int)row.get_Value(portIdIdx);
                                    ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(row);

                                    row = cursor.NextRow();
                                }
                            }

                            foreach (Range r in ranges)
                            {
                                if (r.High > maxPort
                                    || minPort > r.Low)
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Finds ranges of fibers that are not yet spliced or connected
        /// </summary>
        /// <param name="cable">Cable to check</param>
        /// <param name="isFromEnd">Flag of which side of the cable to check</param>
        /// <returns>List of Range</returns>
        public static List<Range> GetAvailableRanges(FiberCableWrapper cable, bool isFromEnd)
        {
            List<Range> result = new List<Range>();

            if (null == cable)
            {
                throw new ArgumentNullException("cable");
            }

            List<int> usedStrands = new List<int>();

            usedStrands.AddRange(GetSplicedStrands(cable, isFromEnd)); // Gets the ones that are spliced to another cable
            usedStrands.AddRange(GetConnectedStrands(cable, isFromEnd)); // Gets the ones that are connected to a device
            usedStrands.Sort();

            List<Range> splicedRanges = MergeRanges(usedStrands);

//            int? fiberCount = GdbUtils.GetDomainedIntName(cable.Feature, ConfigUtil.NumberOfFibersFieldName);
            int? fiberCount = cable.fibers;

            if (null != fiberCount)
            {
                int lowFiber = 1;

                for (int i = 0; i < splicedRanges.Count; i++)
                {
                    Range range = splicedRanges[i];
                    if (lowFiber != range.Low)
                    {
                        // We are good until this range starts
                        result.Add(new Range(lowFiber, range.Low - 1));
                    }

                    lowFiber = range.High + 1;
                }

                if (lowFiber <= fiberCount)
                {
                    result.Add(new Range(lowFiber, (int)fiberCount));
                }
            }

            return result;
        }

        /// <summary>
        /// Finds ranges of ports that are not yet connected
        /// </summary>
        /// <param name="device">Device to check for</param>
        /// <param name="portType">Check input ports or output ports?</param>
        /// <returns>List of Range</returns>
        public static List<Range> GetAvailableRanges(DeviceWrapper device, PortType portType)
        {
            if (null == device)
            {
                throw new ArgumentNullException("device");
            }

            List<int> openPorts = new List<int>();

            openPorts.AddRange(GetOpenPorts(device, portType)); 
            openPorts.Sort();

            List<Range> openRanges = MergeRanges(openPorts);
            return openRanges;
        }

        /// <summary>
        /// Get the existing splice ranges between two cables at an existing closure
        /// </summary>
        /// <param name="cableA">One cable</param>
        /// <param name="cableB">Other cable</param>
        /// <param name="splice">Splice Closure</param>
        /// <returns>List of FiberSplice</returns>
        /// <remarks>Currently only checks A/B as passed, does not check the reverse B/A combination</remarks>
        public static List<FiberSplice> GetSplicedRanges(FiberCableWrapper cableA, FiberCableWrapper cableB, SpliceClosureWrapper splice)
        {
            List<FiberSplice> result = new List<FiberSplice>();
            string spliceWhere = string.Format("{0}='{1}' AND {2}='{3}' AND {4}='{5}'", 
                ConfigUtil.ACableIdFieldName,
                cableA.IPID, 
                ConfigUtil.BCableIdFieldName,
                cableB.IPID, 
                ConfigUtil.SpliceClosureIpidFieldName,
                splice.IPID);

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable = TelecomWorkspaceHelper.Instance().FindTable(ConfigUtil.FiberSpliceTableName);
//                ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable = GdbUtils.GetTable(cableA.Feature.Class, ConfigUtil.FiberSpliceTableName);
                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                releaser.ManageLifetime(filter);

                filter.WhereClause = spliceWhere;
                ((ESRI.ArcGIS.Geodatabase.IQueryFilterDefinition)filter).PostfixClause = string.Format("ORDER BY {0}",ConfigUtil.AFiberNumberFieldName);

                int aUnitIdx = fiberSpliceTable.FindField(ConfigUtil.AFiberNumberFieldName);
                int bUnitIdx = fiberSpliceTable.FindField(ConfigUtil.BFiberNumberFieldName);
                int lossIdx = fiberSpliceTable.FindField(ConfigUtil.LossFieldName);
                int typeIdx = fiberSpliceTable.FindField(ConfigUtil.TypeFieldName);
                ESRI.ArcGIS.Geodatabase.IField typeField = fiberSpliceTable.Fields.get_Field(typeIdx);
                ESRI.ArcGIS.Geodatabase.ICodedValueDomain typeDomain = typeField.Domain as ESRI.ArcGIS.Geodatabase.ICodedValueDomain;

                ESRI.ArcGIS.Geodatabase.ICursor splices = fiberSpliceTable.Search(filter, true);
                releaser.ManageLifetime(splices);

                ESRI.ArcGIS.Geodatabase.IRow spliceRow = splices.NextRow();

                int lastAUnit = -1;
                int lastBUnit = -1;
                double? lastLoss = null;
                object lastType = Type.Missing;

                int aLow = -1;
                int bLow = -1;

                while (null != spliceRow)
                {
                    // These are not-null columns
                    int aUnit = (int)spliceRow.get_Value(aUnitIdx);
                    int bUnit = (int)spliceRow.get_Value(bUnitIdx);
                    
                    object lossObj = spliceRow.get_Value(lossIdx);
                    double? loss = null;
                    if (DBNull.Value != lossObj)
                    {
                        loss = (double)lossObj;
                    }

                    object type = spliceRow.get_Value(typeIdx);
                    
                    if (aUnit != (lastAUnit + 1)
                        || bUnit != (lastBUnit + 1)
                        || loss != lastLoss
                        || !type.Equals(lastType))
                    {
                        if (-1 != lastAUnit)
                        {
                            string typeString = string.Empty;
                            if (null != typeString)
                            {
                                if (null != typeDomain)
                                {
                                    typeString = GdbUtils.GetDomainNameForValue(typeDomain, lastType);
                                }
                                else
                                {
                                    typeString = lastType.ToString(); // DBNull.Value will return string.Empty
                                }
                            }

                            result.Add(new FiberSplice(new Range(aLow, lastAUnit), new Range(bLow, lastBUnit), lastLoss, typeString));
                        }

                        aLow = aUnit;
                        bLow = bUnit;
                    }

                    lastAUnit = aUnit;
                    lastBUnit = bUnit;
                    lastLoss = loss;
                    lastType = type;

                    ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(spliceRow);
                    spliceRow = splices.NextRow();
                }

                if (-1 < aLow)
                {
                    string typeString = string.Empty;
                    if (null != typeString)
                    {
                        if (null != typeDomain)
                        {
                            typeString = GdbUtils.GetDomainNameForValue(typeDomain, lastType);
                        }
                        else
                        {
                            typeString = lastType.ToString(); // DBNull.Value will return string.Empty
                        }
                    }

                    result.Add(new FiberSplice(new Range(aLow, lastAUnit), new Range(bLow, lastBUnit), lastLoss, typeString));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a list of all strand numbers from a cable that are spliced on the given end
        /// </summary>
        /// <param name="cable">Cable to check</param>
        /// <param name="isFromEnd">True to check from end, False to check to end</param>
        /// <returns>List of int</returns>
        private static List<int> GetSplicedStrands(FiberCableWrapper cable, bool isFromEnd)
        {
            if (null == cable)
            {
                throw new ArgumentNullException("cable");
            }

            List<int> result = new List<int>();

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {

                ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable = TelecomWorkspaceHelper.Instance().FindTable(ConfigUtil.FiberSpliceTableName);
//                ESRI.ArcGIS.Geodatabase.ITable fiberSpliceTable = GdbUtils.GetTable(cable.Feature.Class, ConfigUtil.FiberSpliceTableName);
                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                releaser.ManageLifetime(filter);
                filter.WhereClause = string.Format("{0}='{1}' AND {2}='{3}'", 
                    ConfigUtil.ACableIdFieldName,
                    cable.IPID, 
                    ConfigUtil.IsAFromEndFieldName,
                    (isFromEnd ? "T" : "F"));

                ESRI.ArcGIS.Geodatabase.ICursor spliceCursor = fiberSpliceTable.Search(filter, true);
                ESRI.ArcGIS.Geodatabase.IRow spliceRow = spliceCursor.NextRow();
                int fiberIdIdx = fiberSpliceTable.FindField(ConfigUtil.AFiberNumberFieldName);

                while (null != spliceRow)
                {
                    result.Add((int)spliceRow.get_Value(fiberIdIdx));

                    ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(spliceRow);
                    spliceRow = spliceCursor.NextRow();
                }

                ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(spliceCursor);

                filter.WhereClause = string.Format("{0}='{1}' AND {2}='{3}'",
                                    ConfigUtil.BCableIdFieldName,
                                    cable.IPID,
                                    ConfigUtil.IsBFromEndFieldName,
                                    (isFromEnd ? "T" : "F")); spliceCursor = fiberSpliceTable.Search(filter, true);

                spliceRow = spliceCursor.NextRow();
                fiberIdIdx = fiberSpliceTable.FindField(ConfigUtil.BFiberNumberFieldName);

                while (null != spliceRow)
                {
                    result.Add((int)spliceRow.get_Value(fiberIdIdx));

                    ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(spliceRow);
                    spliceRow = spliceCursor.NextRow();
                }

                ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(spliceCursor);
            }

            return result;
        }

        /// <summary>
        /// Gets a list of all port numbers from a device that are not connected on the given end
        /// </summary>
        /// <param name="device">Device to check</param>
        /// <param name="portType">Check input or output ports</param>
        /// <returns>List of int</returns>
        private static List<int> GetOpenPorts(DeviceWrapper device, PortType portType)
        {
            #region Validation
            if (null == device)
            {
                throw new ArgumentNullException("device");
            }

            if (null == device.Feature)
            {
                throw new ArgumentException("device.Class cannot be null");
            }
            #endregion

            List<int> result = new List<int>();

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass = device.Feature.Class as ESRI.ArcGIS.Geodatabase.IFeatureClass;
                ESRI.ArcGIS.Geodatabase.IRelationshipClass deviceHasPorts = ConfigUtil.GetPortRelationship(deviceFtClass);
                if (null != deviceHasPorts)
                {
                    ESRI.ArcGIS.Geodatabase.ITable portTable = deviceHasPorts.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
                    if (null != portTable)
                    {
                        ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                        releaser.ManageLifetime(filter);
                        filter.WhereClause = string.Format("{0}='{1}' AND {2} IS NULL AND {3}='{4}'", 
                            deviceHasPorts.OriginForeignKey,
                            device.Feature.get_Value(deviceFtClass.FindField(deviceHasPorts.OriginPrimaryKey)),
                            ConfigUtil.ConnectedCableFieldName,
                            ConfigUtil.PortTypeFieldName,
                            (PortType.Input == portType ? "1" : "2"));

                        ESRI.ArcGIS.Geodatabase.ICursor portCursor = portTable.Search(filter, true);
                        ESRI.ArcGIS.Geodatabase.IRow portRow = portCursor.NextRow();
                        int portIdIdx = portTable.FindField(ConfigUtil.PortIdFieldName);

                        while (null != portRow)
                        {
                            result.Add((int)portRow.get_Value(portIdIdx));

                            ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(portRow);
                            portRow = portCursor.NextRow();
                        }

                        ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(portCursor);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a list of all strand numbers from a cable that are connected on the given end
        /// </summary>
        /// <param name="cable">Cable to check</param>
        /// <param name="isFromEnd">True to check from end, False to check to end</param>
        /// <returns>List of int</returns>
        private static List<int> GetConnectedStrands(FiberCableWrapper cable, bool isFromEnd)
        {
            #region Validation
            if (null == cable)
            {
                throw new ArgumentNullException("cable");
            }

            #endregion

            List<int> result = new List<int>();

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.IFeatureClass cableFtClass = cable.Feature.Class as ESRI.ArcGIS.Geodatabase.IFeatureClass;

                string[] deviceClassNames = ConfigUtil.DeviceFeatureClassNames;
                for (int i = 0; i < deviceClassNames.Length; i++)
                {
                    string deviceClassName = deviceClassNames[i];
                    ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass = TelecomWorkspaceHelper.Instance().FindFeatureClass(deviceClassName);
//                    ESRI.ArcGIS.Geodatabase.IFeatureClass deviceFtClass = GdbUtils.GetFeatureClass(cableFtClass, deviceClassName);
                    if (null != deviceFtClass)
                    {
                        ESRI.ArcGIS.Geodatabase.IRelationshipClass deviceHasPorts = ConfigUtil.GetPortRelationship(deviceFtClass);
                        if (null != deviceHasPorts)
                        {
                            ESRI.ArcGIS.Geodatabase.ITable portTable = deviceHasPorts.DestinationClass as ESRI.ArcGIS.Geodatabase.ITable;
                            if (null != portTable)
                            {
                                ESRI.ArcGIS.Geodatabase.IQueryFilter filter = new ESRI.ArcGIS.Geodatabase.QueryFilterClass();
                                releaser.ManageLifetime(filter);

                                filter.WhereClause = string.Format("{0}='{1}' AND {2}='{3}' AND {4} IS NOT NULL", 
                                    ConfigUtil.ConnectedCableFieldName,
                                    cable.IPID, 
                                    ConfigUtil.ConnectedEndFieldName,
                                    (isFromEnd ? "T" : "F"),
                                    ConfigUtil.ConnectedFiberFieldName);

                                ESRI.ArcGIS.Geodatabase.ICursor portCursor = portTable.Search(filter, true);
                                ESRI.ArcGIS.Geodatabase.IRow portRow = portCursor.NextRow();
                                int fiberIdIdx = portTable.FindField(ConfigUtil.ConnectedFiberFieldName);

                                while (null != portRow)
                                {
                                    result.Add((int)portRow.get_Value(fiberIdIdx));

                                    ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(portRow);
                                    portRow = portCursor.NextRow();
                                }

                                ESRI.ArcGIS.ADF.ComReleaser.ReleaseCOMObject(portCursor);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if a SpliceClosure has splice connections associated with it, otherwise returns false.
        /// </summary>
        /// <param name="closure">Closure to check</param>
        /// <returns>Boolean</returns>
        public static bool SpliceClosureHasConnections(SpliceClosureWrapper closure)
        {
            #region Validation
            if (null == closure)
            {
                throw new ArgumentNullException("closure");
            }
            #endregion

            bool result = false;

            ESRI.ArcGIS.Geodatabase.IFeatureClass closureFtClass = closure.Feature.Class as ESRI.ArcGIS.Geodatabase.IFeatureClass;

            // Find the SpliceClosure to Splice Relationship class name
            // and test for the number of related obects
            String spliceClosureToSpliceRelClassName = ConfigUtil.SpliceClosureToSpliceRelClassName;
            int featureCount = GdbUtils.GetRelatedObjectCount(closure.Feature, spliceClosureToSpliceRelClassName);
            if (featureCount > 0)
            {
                result = true;
            }

            return result;
        }


    }
}
