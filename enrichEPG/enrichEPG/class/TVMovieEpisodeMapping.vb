﻿#Region "Copyright (C) 2005-2011 Team MediaPortal"

' Copyright (C) 2005-2011 Team MediaPortal
' http://www.team-mediaportal.com
' 
' MediaPortal is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 2 of the License, or
' (at your option) any later version.
' 
' MediaPortal is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
' GNU General Public License for more details.
' 
' You should have received a copy of the GNU General Public License
' along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#End Region

Imports System
Imports System.Collections.Generic
Imports Gentle.Framework
Imports TvDatabase
Namespace TvDatabase
    ''' <summary>
    ''' Instances of this class represent the properties and methods of a row in the table <b>TvMovieEpisodeMapping</b>.
    ''' </summary>
    <TableName("TVMovieEpisodeMapping")> _
    Public Class TVMovieEpisodeMapping
        Inherits Persistent


#Region "Members"

        Private m_isChanged As Boolean

        <TableColumn("idEpisode", NotNull:=True), PrimaryKey(AutoGenerated:=False)> _
        Private m_idEpisode As String

        <TableColumn("idSeries", NotNull:=True)> _
        Private m_idSeries As Integer

        <TableColumn("EPGEpisodeName", NotNull:=False)> _
        Private m_EPGEpisodeName As String

        <TableColumn("seriesNum", NotNull:=True)> _
        Private m_seriesNum As Integer

        <TableColumn("episodeNum", NotNull:=True)> _
        Private m_episodeNum As Integer

#End Region

#Region "Constructors"
        ''' <summary> 
        ''' Create an object from an existing row of data. This will be used by Gentle to 
        ''' construct objects from retrieved rows. 
        ''' </summary> 
        Public Sub New(ByVal idEpisode As String, ByVal idSeries As Integer)
            Me.m_idEpisode = idEpisode
            Me.idSeries = idSeries
        End Sub
#End Region
#Region "Public Properties"

        ''' <summary>
        ''' Indicates whether the entity is changed and requires saving or not.
        ''' </summary>
        Public ReadOnly Property IsChanged() As Boolean
            Get
                Return m_isChanged
            End Get
        End Property

        Public Property idEpisode() As String
            Get
                Return m_idEpisode
            End Get
            Set(ByVal value As String)
                m_isChanged = m_isChanged Or m_idEpisode <> value
                m_idEpisode = value
            End Set
        End Property

        Public Property idSeries() As Integer
            Get
                Return m_idSeries
            End Get
            Set(ByVal value As Integer)
                m_isChanged = m_isChanged Or m_idSeries <> value
                m_idSeries = value
            End Set
        End Property

        Public Property EPGEpisodeName() As String
            Get
                Return m_EPGEpisodeName
            End Get
            Set(ByVal value As String)
                m_isChanged = m_isChanged Or m_EPGEpisodeName <> value
                m_EPGEpisodeName = value
            End Set
        End Property

        Public Property seriesNum() As Integer
            Get
                Return m_seriesNum
            End Get
            Set(ByVal value As Integer)
                m_isChanged = m_isChanged Or m_seriesNum <> value
                m_seriesNum = value
            End Set
        End Property

        Public Property episodeNum() As Integer
            Get
                Return m_episodeNum
            End Get
            Set(ByVal value As Integer)
                m_isChanged = m_isChanged Or m_episodeNum <> value
                m_episodeNum = value
            End Set
        End Property

#End Region

#Region "Storage and Retrieval"

        ''' <summary>
        ''' Static method to retrieve all instances that are stored in the database in one call
        ''' </summary>
        Public Shared Function ListAll() As IList(Of TVMovieEpisodeMapping)
            Return Gentle.Framework.Broker.RetrieveList(Of TVMovieEpisodeMapping)()
        End Function

        ''' <summary>
        ''' Retrieves an entity given it's id.
        ''' </summary>
        Public Overloads Shared Function Retrieve(ByVal idEpisode As String) As TVMovieEpisodeMapping
            Dim key As New Key(GetType(TVMovieEpisodeMapping), True, "idEpisode", idEpisode)
            Return Gentle.Framework.Broker.RetrieveInstance(Of TVMovieEpisodeMapping)(key)
        End Function

        ''' <summary>
        ''' Retrieves an entity given it's id, using Gentle.Framework.Key class.
        ''' This allows retrieval based on multi-column keys.
        ''' </summary>
        Public Overloads Shared Function Retrieve(ByVal key As Key) As TVMovieEpisodeMapping
            Return Gentle.Framework.Broker.RetrieveInstance(Of TVMovieEpisodeMapping)(key)
        End Function

        ''' <summary>
        ''' Persists the entity if it was never persisted or was changed.
        ''' </summary>
        Public Overrides Sub Persist()
            If IsChanged OrElse Not IsPersisted Then
                Try
                    MyBase.Persist()
                Catch ex As Exception
                    MyLog.Error("Exception in TvMovieEpisodeMapping.Persist() with Message {0}", ex.Message)
                    Return
                End Try
                m_isChanged = False
            End If
        End Sub

        Public Sub Delete()
            Dim list As IList(Of TVMovieEpisodeMapping) = ListAll()
            For Each map As TVMovieEpisodeMapping In list
                map.Remove()
            Next
            'Remove()
        End Sub

#End Region

    End Class
End Namespace
