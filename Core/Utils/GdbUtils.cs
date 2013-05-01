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
using ESRI.ArcGIS.Geodatabase;

namespace Esri_Telecom_Tools.Core.Utils
{
    /// <summary>
    /// Helper methods surrounding common geodatabase functions
    /// </summary>
    public static class GdbUtils
    {
        /// <summary>
        /// Gets just the tableName part of a dataset's Name
        /// </summary>
        /// <param name="dataset">IDataset to check</param>
        /// <returns>string</returns>
        public static string ParseTableName(ESRI.ArcGIS.Geodatabase.IDataset dataset)
        {
            if (null == dataset)
            {
                throw new ArgumentNullException("dataset");
            }

            string dbName = string.Empty;
            string tableName = string.Empty;
            string ownerName = string.Empty;

            ESRI.ArcGIS.Geodatabase.ISQLSyntax syntax = (ESRI.ArcGIS.Geodatabase.ISQLSyntax)dataset.Workspace;
            syntax.ParseTableName(dataset.Name, out dbName, out ownerName, out tableName);

            return tableName;
        }

        /// <summary>
        /// Gets a table from the same workspace as any other table
        /// </summary>
        /// <param name="siblingClass">Any object class from the same workspace</param>
        /// <param name="tableName">Name of desired table (no db or owner)</param>
        /// <returns>ESRI.ArcGIS.Geodatabase.ITable</returns>
        //public static ESRI.ArcGIS.Geodatabase.ITable GetTable(ESRI.ArcGIS.Geodatabase.IObjectClass siblingClass, string tableName)
        //{
        //    if (null == siblingClass)
        //    {
        //        throw new ArgumentNullException("siblingClass");
        //    }

        //    if (1 > tableName.Length)
        //    {
        //        throw new ArgumentException("tableName not specified");
        //    }
        //    ESRI.ArcGIS.Geodatabase.IDataset dataset = (ESRI.ArcGIS.Geodatabase.IDataset)siblingClass;
        //    ESRI.ArcGIS.Geodatabase.IFeatureWorkspace workspace = (ESRI.ArcGIS.Geodatabase.IFeatureWorkspace)dataset.Workspace;
        //    ESRI.ArcGIS.Geodatabase.ISQLSyntax sqlSyntax = (ESRI.ArcGIS.Geodatabase.ISQLSyntax)workspace;

        //    string owner = string.Empty;
        //    string table = string.Empty;
        //    string db = string.Empty;

        //    // Get the db and owner from the sibling class
        //    sqlSyntax.ParseTableName(dataset.Name, out db, out owner, out table);

        //    // Use that to qualify the requested tableName 
        //    string qualifiedName = sqlSyntax.QualifyTableName(db, owner, tableName);
        //    return workspace.OpenTable(qualifiedName);
        //}

        /// <summary>
        /// Gets a feature class from the same workspace as any other table
        /// </summary>
        /// <param name="siblingClass">Any object class from the same workspace</param>
        /// <param name="tableName">Name of desired table (no db or owner)</param>
        /// <returns>ESRI.ArcGIS.Geodatabase.IFeatureClass</returns>
        //public static ESRI.ArcGIS.Geodatabase.IFeatureClass GetFeatureClass(ESRI.ArcGIS.Geodatabase.IObjectClass siblingClass, string ftClassName)
        //{
        //    if (null == siblingClass)
        //    {
        //        throw new ArgumentException("siblingClass");
        //    }

        //    if (1 > ftClassName.Length)
        //    {
        //        throw new ArgumentException("ftClassName");
        //    }

        //    return GdbUtils.GetTable(siblingClass, ftClassName) as ESRI.ArcGIS.Geodatabase.IFeatureClass;
        //}

        /// <summary>
        /// Gets a feature class from a workspace 
        /// </summary>
        /// <param name="workspace">Workspace in which FC exists</param>
        /// <param name="tableName">Name of desired table (no db or owner)</param>
        /// <returns>ESRI.ArcGIS.Geodatabase.IFeatureClass</returns>
        //public static ESRI.ArcGIS.Geodatabase.IFeatureClass GetFeatureClass(IFeatureWorkspace workspace, string ftClassName)
        //{
        //    if (null == workspace)
        //    {
        //        throw new ArgumentException("workspace");
        //    }

        //    if (1 > ftClassName.Length)
        //    {
        //        throw new ArgumentException("ftClassName");
        //    }

        //    return workspace.OpenFeatureClass(ftClassName);
        //}

        /// <summary>
        /// Gets a relationship class from the same workspace as any other object class
        /// </summary>
        /// <param name="siblingClass">Any object class from the same workspace</param>
        /// <param name="tableName">Name of desired relationship class (no db or owner)</param>
        /// <returns>ESRI.ArcGIS.Geodatabase.IRelationshipClass</returns>
        public static ESRI.ArcGIS.Geodatabase.IRelationshipClass GetRelationshipClass(ESRI.ArcGIS.Geodatabase.IObjectClass siblingClass, string name)
        {
            if (null == siblingClass)
            {
                throw new ArgumentNullException("siblingClass");
            }

            if (1 > name.Length)
            {
                throw new ArgumentException("name");
            }

            ESRI.ArcGIS.Geodatabase.IDataset dataset = (ESRI.ArcGIS.Geodatabase.IDataset)siblingClass;
            ESRI.ArcGIS.Geodatabase.IFeatureWorkspace workspace = (ESRI.ArcGIS.Geodatabase.IFeatureWorkspace)dataset.Workspace;
            ESRI.ArcGIS.Geodatabase.ISQLSyntax sqlSyntax = (ESRI.ArcGIS.Geodatabase.ISQLSyntax)workspace;

            string owner = string.Empty;
            string tableName = string.Empty;
            string db = string.Empty;

            // Get the db and owner from the sibling class
            sqlSyntax.ParseTableName(dataset.Name, out db, out owner, out tableName);

            // Qualify the requested relationship class name using that db and owner
            string qualifiedName = sqlSyntax.QualifyTableName(db, owner, name);
            return workspace.OpenRelationshipClass(qualifiedName);
        }


        /// <summary>
        /// Finds the actual value of a given name of a coded value domain 
        /// </summary>
        /// <param name="domain">CodedValueDomain to check</param>
        /// <param name="name">Name of the desired value</param>
        /// <returns>The value</returns>
        public static object GetDomainValueForName(ESRI.ArcGIS.Geodatabase.ICodedValueDomain domain, string name)
        {
            object result = null;

            if (null == domain)
            {
                throw new ArgumentNullException("domain");
            }

            for (int i = 0; i < domain.CodeCount; i++)
            {
                string testName = domain.get_Name(i);
                if (0 == string.Compare(name, testName))
                {
                    result = domain.get_Value(i);
                    break;
                }
            }

            if (null == result)
            {
                throw new Exception(string.Format("name {0} not found in the domain {1}", name, ((ESRI.ArcGIS.Geodatabase.IDomain)domain).Name));
            }

            return result;
        }

        /// <summary>
        /// Finds the display name for a given value in a coded value domain 
        /// </summary>
        /// <param name="domain">CodedValueDomain to check</param>
        /// <param name="value">The value</param>
        /// <returns>The name, or string.Empty if not found</returns>
        public static string GetDomainNameForValue(ESRI.ArcGIS.Geodatabase.ICodedValueDomain domain, object value)
        {
            string result = string.Empty;
            
            if (null == domain)
            {
                throw new ArgumentNullException("domain");
            }

            for (int i = 0; i < domain.CodeCount; i++)
            {
                object testValue = domain.get_Value(i);
                if (testValue.Equals(value))
                {
                    result = domain.get_Name(i);
                    break;
                }
            }


            if (null == result)
            {
                throw new Exception(string.Format("value {0} not found in the domain {1}", value, ((ESRI.ArcGIS.Geodatabase.IDomain)domain).Name));
            }

            return result;
        }

        /// <summary>
        /// Returns a value as verified against a coded value domain. If there is not a coded value domain, just returns the value
        /// </summary>
        /// <param name="field">Field to check against</param>
        /// <param name="value">Value to check</param>
        /// <returns>True value</returns>
        public static object CheckForCodedValue(ESRI.ArcGIS.Geodatabase.IField field, object value)
        {
            // Assume no domain, in which case we will just return the input value
            object result = value;

            if (null == field)
            {
                throw new ArgumentNullException("field");
            }

            ESRI.ArcGIS.Geodatabase.ICodedValueDomain domain = field.Domain as ESRI.ArcGIS.Geodatabase.ICodedValueDomain;
            if (null != domain)
            {
                result = GetDomainValueForName(domain, value.ToString());
            }
            
            return result;
        }

        /// <summary>
        /// Returns a display name as verified against a coded value domain. If there is not a coded value domain, just returns 
        /// the value
        /// </summary>
        /// <param name="field">Field to check against</param>
        /// <param name="value">Value to check</param>
        /// <returns>Display name</returns>
        public static string CheckForCodedName(ESRI.ArcGIS.Geodatabase.IField field, object value)
        {
            // Assume no domain, in which case we will just return the input value
            string result = value.ToString();

            if (null == field)
            {
                throw new ArgumentNullException("field");
            }

            ESRI.ArcGIS.Geodatabase.ICodedValueDomain domain = field.Domain as ESRI.ArcGIS.Geodatabase.ICodedValueDomain;
            if (null != domain)
            {
                result = GetDomainNameForValue(domain, value);
            }

            return result;
        }

        /// <summary>
        /// Returns a nullable int value for a given field on a feature. If the field uses a coded value domain, the name for
        /// the value is returned.
        /// </summary>
        /// <param name="feature">IFeature</param>
        /// <param name="fieldName">Name of the field that holds the value</param>
        /// <returns>int?</returns>
        public static int? GetDomainedIntName(ESRI.ArcGIS.Geodatabase.IFeature feature, string fieldName)
        {
            int? result = null;

            #region Validation

            if (null == feature)
            {
                throw new ArgumentNullException("feature");
            }

            int fieldIdx = feature.Fields.FindField(fieldName);
            if (-1 == fieldIdx)
            {
                string message = string.Format("Field {0} does not exist.", fieldName);
                throw new ArgumentException(message);
            }

            #endregion

            object objValue = feature.get_Value(fieldIdx);
            if (DBNull.Value != objValue)
            {
                ESRI.ArcGIS.Geodatabase.IField field = feature.Fields.get_Field(fieldIdx);
                string valueString = CheckForCodedName(field, objValue);

                int parseResult = -1;
                if (!int.TryParse(valueString, out parseResult))
                {
                    string message = string.Format("{0} value {1} could not be parsed to int.", fieldName, valueString);
                    throw new Exception(message);
                }
                else
                {
                    result = parseResult;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns all features from a given feature class that have a vertex or endpoint coincident with a given point 
        /// </summary>
        /// <param name="point">IPoint to use as the spatial filter</param>
        /// <param name="searchFtClass">IFeatureClass to search in</param>
        /// <param name="linearEndpointsOnly">Flag to use only the endpoints of a line instead of all vertices</param>
        /// <returns>List of IFeature</returns>
        public static List<ESRI.ArcGIS.Geodatabase.IFeature> GetLinearsWithCoincidentEndpoints(ESRI.ArcGIS.Geometry.IPoint point, ESRI.ArcGIS.Geodatabase.IFeatureClass searchFtClass)
        {
            return GetFeaturesWithCoincidentVertices(point, searchFtClass, true, 0);
        }

        /// <summary>
        /// Returns all features from a given feature class that have a vertex or endpoint coincident with a given point 
        /// </summary>
        /// <param name="point">IPoint to use as the spatial filter</param>
        /// <param name="searchFtClass">IFeatureClass to search in</param>
        /// <param name="linearEndpointsOnly">Flag to use only the endpoints of a line instead of all vertices</param>
        /// <param name="buffer">Search geometry buffer in map units</param>
        /// <returns>List of IFeature</returns>
        public static List<ESRI.ArcGIS.Geodatabase.IFeature> GetLinearsWithCoincidentEndpoints(ESRI.ArcGIS.Geometry.IPoint point, ESRI.ArcGIS.Geodatabase.IFeatureClass searchFtClass, double buffer)
        {
            return GetFeaturesWithCoincidentVertices(point, searchFtClass, true, buffer);
        }

        /// <summary>
        /// Returns all features from a given feature class that have a vertex or endpoint coincident with a given point 
        /// </summary>
        /// <param name="point">IPoint to use as the spatial filter</param>
        /// <param name="searchFtClass">IFeatureClass to search in</param>
        /// <param name="linearEndpointsOnly">Flag to use only the endpoints of a line instead of all vertices</param>
        /// <param name="buffer">Search geometry buffer in map units</param>
        /// <returns>List of IFeature</returns>
        public static List<ESRI.ArcGIS.Geodatabase.IFeature> GetFeaturesWithCoincidentVertices(ESRI.ArcGIS.Geometry.IPoint point, ESRI.ArcGIS.Geodatabase.IFeatureClass searchFtClass, bool linearEndpointsOnly, double buffer)
        {
            List<ESRI.ArcGIS.Geodatabase.IFeature> result = new List<ESRI.ArcGIS.Geodatabase.IFeature>();

            using (ESRI.ArcGIS.ADF.ComReleaser releaser = new ESRI.ArcGIS.ADF.ComReleaser())
            {
                ESRI.ArcGIS.Geodatabase.ISpatialFilter filter = new ESRI.ArcGIS.Geodatabase.SpatialFilterClass();
                releaser.ManageLifetime(filter);

                ESRI.ArcGIS.Geometry.IEnvelope filterGeometry = point.Envelope;
                if (0 < buffer)
                {
                    filterGeometry.Expand(buffer, buffer, false);
                }

                filter.SpatialRel = ESRI.ArcGIS.Geodatabase.esriSpatialRelEnum.esriSpatialRelIntersects;
                filter.Geometry = filterGeometry;

                ESRI.ArcGIS.Geodatabase.IFeatureCursor fts = searchFtClass.Search(filter, false);
                releaser.ManageLifetime(fts);

                ESRI.ArcGIS.Geodatabase.IFeature ft = fts.NextFeature();
                while (null != ft)
                {
                    if (searchFtClass.ShapeType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint)
                    {
                        result.Add(ft);
                    }
                    else if (searchFtClass.ShapeType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolyline && linearEndpointsOnly)
                    {
                        ESRI.ArcGIS.Geometry.IPolyline polyline = (ESRI.ArcGIS.Geometry.IPolyline)ft.Shape;
                        ESRI.ArcGIS.Geometry.IRelationalOperator fromPoint = polyline.FromPoint as ESRI.ArcGIS.Geometry.IRelationalOperator;
                        ESRI.ArcGIS.Geometry.IRelationalOperator toPoint = polyline.ToPoint as ESRI.ArcGIS.Geometry.IRelationalOperator;

                        if (fromPoint.Equals(point) || toPoint.Equals(point))
                        {
                            result.Add(ft);
                        }
                    }
                    else
                    {
                        ESRI.ArcGIS.Geometry.IPointCollection pointCollection = ft.Shape as ESRI.ArcGIS.Geometry.IPointCollection;
                        if (null != pointCollection)
                        {
                            for (int i = 0; i < pointCollection.PointCount; i++)
                            {
                                ESRI.ArcGIS.Geometry.IRelationalOperator testPoint = pointCollection.get_Point(i) as ESRI.ArcGIS.Geometry.IRelationalOperator;
                                if (testPoint.Equals(point))
                                {
                                    result.Add(ft);
                                    break;
                                }
                            }
                        }
                    }

                    ft = fts.NextFeature();
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the count of related objects for a given object
        /// </summary>
        /// <param name="anObject">Object to check</param>
        /// <param name="relationshipClassName">Relationship class to check</param>
        /// <returns>int</returns>
        public static int GetRelatedObjectCount(ESRI.ArcGIS.Geodatabase.IObject anObject, string relationshipClassName)
        {
            int result = 0;

            if (null == anObject)
            {
                throw new ArgumentNullException("anObject");
            }

            if (1 > relationshipClassName.Length)
            {
                throw new ArgumentException("relationshipClassName not specified.");
            }

            ESRI.ArcGIS.Geodatabase.IRelationshipClass relationshipClass = GetRelationshipClass(anObject.Class, relationshipClassName);
            if (null == relationshipClass)
            {
                throw new Exception(string.Format("Relationship class {0} could not be found.", relationshipClassName));
            }

            ESRI.ArcGIS.esriSystem.ISet relatedObjects = relationshipClass.GetObjectsRelatedToObject(anObject);
            result = relatedObjects.Count;
            return result;
        }

        /// <summary>
        /// Determines the coincident ends of the given edges and returns the junction's shape as IPoint
        /// </summary>
        /// <param name="featureA">One edge feature</param>
        /// <param name="featureB">Another edge feature</param>
        /// <returns>IPoint</returns>
        public static ESRI.ArcGIS.Geometry.IPoint GetJunctionPoint(ESRI.ArcGIS.Geodatabase.IEdgeFeature featureA, ESRI.ArcGIS.Geodatabase.IEdgeFeature featureB)
        {
            #region Validation

            if (null == featureA)
            {
                throw new ArgumentNullException("featureA");
            }

            if (null == featureB)
            {
                throw new ArgumentNullException("featureB");
            }

            #endregion

            int aFromEid = featureA.FromJunctionEID;
            int aToEid = featureA.ToJunctionEID;
            int bFromEid = featureB.FromJunctionEID;
            int bToEid = featureB.ToJunctionEID;

            ESRI.ArcGIS.Geodatabase.IJunctionFeature junctionFt = null;
            if (aFromEid == bFromEid || aFromEid == bToEid)
            {
                junctionFt = featureA.FromJunctionFeature;
            }
            else if (aToEid == bFromEid || aToEid == bToEid)
            {
                junctionFt = featureA.ToJunctionFeature;
            }

            if (null == junctionFt)
            {
                throw new Exception("Unable to find junction feature for provided edges.");
            }

            ESRI.ArcGIS.Geometry.IGeometry geometry  = ((ESRI.ArcGIS.Geodatabase.IFeature)junctionFt).ShapeCopy;
            if (geometry.GeometryType != ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint)
            {
                throw new Exception("Junction feature's geometry is not a point.");
            }

            return geometry as ESRI.ArcGIS.Geometry.IPoint;
        }

        /// <summary>
        /// Gets a property set of name values converted to string 
        /// for a given feature or row.
        /// </summary>
        /// <param name="row">One row feature to descrive</param>
        /// <returns>List</returns>
        public static List<NameValuePair> PropertySet(IRow row)
        {
            String subtypeFieldName = string.Empty;
            ISubtypes subtypes = row.Table as ISubtypes;

            List<NameValuePair> result = new List<NameValuePair>();

            IFields fields = row.Fields;
            int count = fields.FieldCount;

            // -------------------------------------
            // For each field check the types and 
            // do the appropriate thing to convert 
            // to a string representation for 
            // display purposes.
            // 
            // Also need to deal with domain values.
            // --------------------------------------
            for (int idx = 0; idx < count; idx++)
            {
                // get name and alias
                IField field = fields.get_Field(idx);
                IDomain domain = field.Domain;
                string alias = field.AliasName;
                string name = field.Name;
                string value = string.Empty;

                // Are we dealing with a subtype field?
                if (subtypes.HasSubtype && 
                    (0 == string.Compare(subtypes.SubtypeFieldName,field.Name,true)))
                {
                    IRowSubtypes rowSubTypes = row as IRowSubtypes;
                    value = subtypes.SubtypeName[rowSubTypes.SubtypeCode];
                }
                else if (domain != null) // field has domain
                {
                    if (domain is ICodedValueDomain)
                    {
                        // If field type is text
                        if (field.Type == esriFieldType.esriFieldTypeString 
                            || field.Type == esriFieldType.esriFieldTypeInteger)
                        {
                            value = GdbUtils.CheckForCodedName(field, row.get_Value(idx));
                        }
                    }
                    else  // Its a range domain (numeric only)
                    {
                        // Can just use value as is
                        value = row.get_Value(idx).ToString();
                    }
                }
                else  // non domain or subtype field, just get string representation
                {
                    value = row.get_Value(idx).ToString();
                }

                NameValuePair item = new NameValuePair(alias, name, value);
                result.Add(item);
            }
            return result;
        }


    }
}
