﻿// ***********************************************************************
// Assembly         : TSharp.Core
// Author           : tangjingbo
// Created          : 08-21-2013
//
// Last Modified By : tangjingbo
// Last Modified On : 08-21-2013
// ***********************************************************************
// <copyright file="CommandBuilder.Update.cs" company="Extendsoft">
//     Copyright (c) Extendsoft. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using EmitMapper;
using EmitMapper.Mappers;
using EmitMapper.MappingConfiguration;
using EmitMapper.MappingConfiguration.MappingOperations;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using LightDataAccess.MappingConfigs;

namespace LightDataAccess
{
    /// <summary>
    /// Class CommandBuilder
    /// </summary>
	public static partial class CommandBuilder
    {
        /// <summary>
        /// Builds the update operator.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="idFieldNames">The id field names.</param>
        /// <param name="dbSettings">The db settings.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
		public static bool BuildUpdateOperator(
            this DbCommand cmd,
            object obj,
            string tableName,
            string[] idFieldNames,
            DbSettings dbSettings
        )
        {
            return BuildUpdateCommand(cmd, obj, tableName, idFieldNames, null, null, null, dbSettings);
        }

        /// <summary>
        /// Builds the update command.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="idFieldNames">The id field names.</param>
        /// <param name="includeFields">The include fields.</param>
        /// <param name="excludeFields">The exclude fields.</param>
        /// <param name="changeTracker">The change tracker.</param>
        /// <param name="dbSettings">The db settings.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
		public static bool BuildUpdateCommand(
            this DbCommand cmd,
            object obj,
            string tableName,
            IEnumerable<string> idFieldNames,
            IEnumerable<string> includeFields,
            IEnumerable<string> excludeFields,
            ObjectsChangeTracker changeTracker,
            DbSettings dbSettings
        )
        {
            if (idFieldNames == null)
            {
                idFieldNames = new string[0];
            }
            idFieldNames = idFieldNames.Select(n => n.ToUpper()).ToArray();

            if (changeTracker != null)
            {
                ObjectsChangeTracker.TrackingMember[] changedFields = changeTracker.GetChanges(obj);
                if (changedFields != null)
                {
                    if (includeFields == null)
                    {
                        includeFields = changedFields.Select(c => c.name).ToArray();
                    }
                    else
                    {
                        includeFields = includeFields.Intersect(changedFields.Select(c => c.name)).ToArray();
                    }
                }
            }
            if (includeFields != null)
            {
                includeFields = includeFields.Concat(idFieldNames);
            }
            IMappingConfigurator config = new AddDbCommandsMappingConfig(
                    dbSettings,
                    includeFields,
                    excludeFields,
                    "updateop_inc_" + includeFields.ToCSV("_") + "_exc_" + excludeFields.ToCSV("_")
            );

            ObjectsMapperBaseImpl mapper = ObjectMapperManager.DefaultInstance.GetMapperImpl(
                obj.GetType(),
                typeof(DbCommand),
                config
            );

            string[] fields = mapper
                .StroredObjects
                .OfType<SrcReadOperation>()
                .Select(m => m.Source.MemberInfo.Name)
                .Where(f => !idFieldNames.Contains(f))
                .ToArray();

            if (fields.Length == 0)
            {
                return false;
            }

            string cmdStr =
                "UPDATE " +
                tableName +
                " SET " +
                fields
                    .Select(
                        f => dbSettings.GetEscapedName(f.ToUpper()) + "=" + dbSettings.GetParamName(f.ToUpper())
                    )
                    .ToCSV(",") +
                " WHERE " +
                idFieldNames.Select(fn => dbSettings.GetEscapedName(fn) + "=" + dbSettings.GetParamName(fn)).ToCSV(" AND ")
                ;
            cmd.CommandText = cmdStr;
            cmd.CommandType = System.Data.CommandType.Text;

            mapper.Map(obj, cmd, null);
            return true;
        }
    }
}
