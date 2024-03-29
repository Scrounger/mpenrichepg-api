﻿Imports TvDatabase
Imports System.IO
Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.OleDb
Imports System.Diagnostics
Imports System.Text
Imports System.Threading
Imports System.Windows.Forms
Imports System.Text.RegularExpressions
Imports enrichEPG.TvDatabase
Imports enrichEPG.Database
Imports Gentle.Framework


Public Class IdentifySeries

#Region "Members"
    Private Shared _SeriesEN As TvdbLib.Data.TvdbSeries
    Private Shared _SeriesLang As TvdbLib.Data.TvdbSeries
    Private Shared _TheTvDbEpisode As TvdbLib.Data.TvdbEpisode
    Private Shared _UpdateEpgEpisodeSeriesNameCounter As Integer
    Private Shared _idSeries As Integer
    Private Shared _logLevenstein As String
    Private Shared _TvSeriesEpisode As MyTvSeries.MyEpisode
#End Region

#Region "Properties"
    Public Shared Property SeriesEN() As TvdbLib.Data.TvdbSeries
        Get
            Return _SeriesEN
        End Get
        Set(ByVal value As TvdbLib.Data.TvdbSeries)
            _SeriesEN = value
        End Set
    End Property
    Public Shared Property SeriesLang() As TvdbLib.Data.TvdbSeries
        Get
            Return _SeriesLang
        End Get
        Set(ByVal value As TvdbLib.Data.TvdbSeries)
            _SeriesLang = value
        End Set
    End Property
    Public Shared Property TheTvDbEpisode() As TvdbLib.Data.TvdbEpisode
        Get
            Return _TheTvDbEpisode
        End Get
        Set(ByVal value As TvdbLib.Data.TvdbEpisode)
            _TheTvDbEpisode = value
        End Set
    End Property
    Public Shared Property TvSeriesEpisode() As MyTvSeries.MyEpisode
        Get
            Return _TvSeriesEpisode
        End Get
        Set(ByVal value As MyTvSeries.MyEpisode)
            _TvSeriesEpisode = value
        End Set
    End Property
    Public Shared Property UpdateEpgEpisodeSeriesNameCounter() As Integer
        Get
            Return _UpdateEpgEpisodeSeriesNameCounter
        End Get
        Set(ByVal value As Integer)
            _UpdateEpgEpisodeSeriesNameCounter = value
        End Set
    End Property
    Public Shared Property logLevenstein() As String
        Get
            Return _logLevenstein
        End Get
        Set(ByVal value As String)
            _logLevenstein = value
        End Set
    End Property
#End Region

#Region "Functions"
    Public Shared Sub MarkEpgEpisodeAsNew(ByVal program As Program, ByVal EpisodeExistsLocal As Boolean)
        Try
            Select Case MySettings.ClickfinderProgramGuideImportEnable
                Case Is = False
                    'ohne Clickfinder
                    If EpisodeExistsLocal = False Then
                        If InStr(program.Description, MySettings.NewEpisodeString) = 0 Then
                            program.Description = MySettings.NewEpisodeString & vbNewLine & program.Description
                        ElseIf InStr(program.Description, MySettings.EpisodeExistsString) > 0 Then
                            program.Description = Replace(program.Description, MySettings.EpisodeExistsString, MySettings.NewEpisodeString)
                        End If
                    Else
                        If InStr(program.Description, MySettings.EpisodeExistsString) = 0 Then
                            program.Description = MySettings.EpisodeExistsString & vbNewLine & program.Description
                        ElseIf InStr(program.Description, MySettings.NewEpisodeString) > 0 Then
                            program.Description = Replace(program.Description, MySettings.NewEpisodeString, MySettings.EpisodeExistsString)
                        End If
                    End If

                Case Is = True
                    'mit Clickfinder
                    If EpisodeExistsLocal = False Then
                        If InStr(program.Description, "Neue Folge: " & program.EpisodeName) = 0 Then
                            program.Description = Replace(program.Description, "Folge: " & program.EpisodeName, "Neue Folge: " & program.EpisodeName)
                        End If
                    Else
                        If InStr(program.Description, "Neue Folge: " & program.EpisodeName) > 0 Then
                            program.Description = Replace(program.Description, "Neue Folge: " & program.EpisodeName, "Folge: " & program.EpisodeName)
                        End If
                    End If
            End Select

            program.Persist()


        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [IdentifySeries] [MarkEpgEpisodeAsNew]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try
    End Sub
    Private Shared Sub UpdateEpgEpisode(ByVal program As Program, ByVal TvSeries As MyTvSeries, ByVal Episode As MyTvSeries.MyEpisode, ByVal episodeIdentified As Boolean)
        Try
            'Daten im EPG (program) updaten

            If episodeIdentified = True Then
                program.SeriesNum = CStr(Episode.SeriesNum)
                program.EpisodeNum = CStr(Episode.EpisodeNum)
                program.StarRating = Episode.Rating
            Else
                'program.SeriesNum = String.Empty
                'program.EpisodeNum = String.Empty
                program.StarRating = TvSeries.Rating
            End If

            UpdateEpgEpisodeSeriesName(program, TvSeries.Title)

        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [IdentifySeries] [UpdateEpgEpisode]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try
    End Sub
    Private Shared Sub UpdateEpgEpisodeSeriesName(ByVal program As Program, ByVal TvSeriesDBname As String)
        Try
            'Daten im EPG (program) updaten
            If Not TvSeriesDBname = program.Title Then
                program.Title = TvSeriesDBname

                UpdateEpgEpisodeSeriesNameCounter = UpdateEpgEpisodeSeriesNameCounter + 1
            End If


        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [IdentifySeries] [UpdateEpgEpisode2SeriesName]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try
    End Sub
    Private Shared Sub UpdateTvMovieProgram(ByVal program As Program, ByVal TvSeries As MyTvSeries, ByVal Episode As MyTvSeries.MyEpisode, ByVal EpisodeExistsLocal As Boolean, ByVal episodeIdentified As Boolean)
        Try
            If MySettings.ClickfinderProgramGuideImportEnable = True Then
                'Zunächst nur Serien Infos schreiben (episode in TvSeriesDB nicht gefunden)


                ''Prüfen ob channel gemappt ist
                'Dim _SQLstring As String = "Select * from tvmoviemapping where idChannel = " & program.IdChannel
                'Dim _SQLstate2 As SqlStatement = Broker.GetStatement(_SQLstring)
                'Dim _Mapped As List(Of TvMovieMapping) = ObjectFactory.GetCollection(GetType(TvMovieMapping), _SQLstate2.Execute())


                'ggf. einen TvMovieProgram Eintrag erstellen, wenn Source z.B. Epg grab
                Try
                    Dim _TvMovieProgramTest As TVMovieProgram = TVMovieProgram.Retrieve(program.IdProgram)
                Catch ex As Exception
                    Dim _newTvMovieProgram As New TVMovieProgram(program.IdProgram)
                    _newTvMovieProgram.Persist()
                End Try


                'idProgram in TvMovieProgram suchen & Daten aktualisieren
                Dim _TvMovieProgram As TVMovieProgram = TVMovieProgram.Retrieve(program.IdProgram)
                _TvMovieProgram.idSeries = TvSeries.idSeries
                _TvMovieProgram.local = EpisodeExistsLocal
                _TvMovieProgram.TVMovieBewertung = 6

                'Serien Poster Image
                If Not String.IsNullOrEmpty(TvSeries.PosterImage) Then _TvMovieProgram.SeriesPosterImage = TvSeries.PosterImage
                'FanArt
                If Not String.IsNullOrEmpty(TvSeries.FanArt) Then _TvMovieProgram.FanArt = TvSeries.FanArt

                'Falls Episode geladen
                If episodeIdentified = True Then
                    'idEpisode
                    If Not String.IsNullOrEmpty(Episode.idEpisode) Then _TvMovieProgram.idEpisode = Episode.idEpisode
                    'FileName
                    If Not String.IsNullOrEmpty(Episode.EpisodeFilename) Then _TvMovieProgram.FileName = Episode.EpisodeFilename
                    'Episoden Image
                    If Not String.IsNullOrEmpty(Episode.ThumbFilename) Then _TvMovieProgram.EpisodeImage = Episode.ThumbFilename
                Else
                    _TvMovieProgram.idEpisode = String.Empty
                End If

                'MyLog.Info("{0}, {1},idseries: {2},idepisode: {3}, local: {4}", program.Title, program.EpisodeName, _TvMovieProgram.idSeries, _TvMovieProgram.idEpisode, _TvMovieProgram.local)

                _TvMovieProgram.Persist()

            End If

        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [IdentifySeries] [UpdateTvMovieProgram]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try
    End Sub
    Public Shared Function UpdateProgramAndTvMovieProgram(ByVal program As Program, ByVal TvSeries As MyTvSeries, ByVal Episode As MyTvSeries.MyEpisode, ByVal EpisodeExistLocal As Boolean, ByVal episodeIdentfiy As Boolean) As Boolean
        Try
            'False: lokal = 0 
            'True: local = 1
            Dim _Local As Boolean = EpisodeExistLocal

            'minSeasonNum / disabled
            Try
                Dim _Mapping As TvMovieSeriesMapping = TvMovieSeriesMapping.Retrieve(TvSeries.idSeries)

                'TvMovieSeriesMapping: minSeasonNum
                If (Episode.SeriesNum < _Mapping.minSeasonNum And episodeIdentfiy = True) Or _Mapping.disabled = True Then
                    _Local = True
                End If
            Catch ex As Exception
                _Local = EpisodeExistLocal
            End Try

            'Daten im EPG (program) updaten
            IdentifySeries.UpdateEpgEpisode(program, TvSeries, Episode, episodeIdentfiy)

            'Neue Episode -> im EPG Describtion kennzeichnen
            IdentifySeries.MarkEpgEpisodeAsNew(program, _Local)

            'Clickfinder ProgramGuide Infos in TvMovieProgram schreiben, sofern aktiviert
            IdentifySeries.UpdateTvMovieProgram(program, TvSeries, Episode, _Local, episodeIdentfiy)

            Return _Local

        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [IdentifySeries] [UpdateProgramAndTvMovieProgram]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try
    End Function

    'Nach Episoden suchen und updaten
    Public Shared Function EpisodeIdentifed(ByVal program As Program, ByVal TvSeries As MyTvSeries) As Boolean
        TvSeriesEpisode = Nothing

        Dim _programIdentified As Boolean = False

        '---------------- TvSeries DB --------------------------------
        Try
            Dim _Episode As MyTvSeries.MyEpisode = TvSeries.Episode(program.EpisodeName)
            _programIdentified = True
            TvSeriesEpisode = _Episode

            TvSeriesEpisode.ExistLocal = UpdateProgramAndTvMovieProgram(program, TvSeries, _Episode, _Episode.ExistLocal, True)

            MyLog.Info("enrichEPG: [IdentifySeries] [EpisodeIdentifed]: TvSeriesDB: S{0}E{1} - {2} (newEpisode: {3}{4})", _
            _Episode.SeriesNum, _Episode.EpisodeNum, program.EpisodeName, Not TvSeriesEpisode.ExistLocal, MyTvSeries.Helper.logLevenstein)

            Return True
        Catch ex As Exception
            _programIdentified = False
            'Falls Episode nicht gefunden wird, Exception abfangen
        End Try


        '---------------- TheTvDB --------------------------------
        'auf TheTvDb.com nach episode suchen (language: lang)
        If _programIdentified = False And MySettings.useTheTvDb = True Then
            If IdentifySeries.TheTvDbEpisodeIdentifed(program) = True Then
                'Episode auf TheTvDb gefunden
                Dim _Episode As MyTvSeries.MyEpisode = TvSeries.Episode(IdentifySeries.TheTvDbEpisode.SeasonNumber, IdentifySeries.TheTvDbEpisode.EpisodeNumber)
                _programIdentified = True
                TvSeriesEpisode = _Episode

                TvSeriesEpisode.ExistLocal = IdentifySeries.UpdateProgramAndTvMovieProgram(program, TvSeries, _Episode, _Episode.ExistLocal, True)

                MyLog.Info("enrichEPG: [IdentifySeries] [EpisodeIdentifed]: TheTvDb.com ({0}): S{1}E{2} - {3} (newEpisode: {4}{5})", IdentifySeries.TheTvDbEpisode.Language.Abbriviation, _
                                          IdentifySeries.TheTvDbEpisode.SeasonNumber, IdentifySeries.TheTvDbEpisode.EpisodeNumber, program.EpisodeName, Not TvSeriesEpisode.ExistLocal, IdentifySeries.logLevenstein)
                Return True
            End If
        End If

        '---------------- TvMovieEpisodeMapping --------------------------------
        'Verlinkung in TvMovieEpisodeMapping suchen
        If _programIdentified = False Then

            'Nach Verlinkung suchen
            'Zunächst nach alle Serien mit SerienName aus TvSeries DB suchen
            Dim _SQLString As String = "Select * from TvMovieEpisodeMapping " & _
            "WHERE idSeries = " & TvSeries.idSeries & " " & _
            "AND EPGEpisodeName LIKE '%" & MyTvSeries.Helper.allowedSigns(program.EpisodeName) & "%'"

            '_SQLString = Replace(_SQLString, " * ", " Program.IdProgram, Program.Classification, Program.Description, Program.EndTime, Program.EpisodeName, Program.EpisodeNum, Program.EpisodePart, Program.Genre, Program.IdChannel, Program.OriginalAirDate, Program.ParentalRating, Program.SeriesNum, Program.StarRating, Program.StartTime, Program.state, Program.Title ")
            Dim _SQLstate2 As SqlStatement = Broker.GetStatement(_SQLString)
            Dim _EpisodeMappingList As List(Of TVMovieEpisodeMapping) = ObjectFactory.GetCollection(GetType(TVMovieEpisodeMapping), _SQLstate2.Execute())
            'Mapping EpisodeName gefunden
            If _EpisodeMappingList.Count > 0 Then

                Dim _Episode As MyTvSeries.MyEpisode = TvSeries.Episode(_EpisodeMappingList(0).seriesNum, _EpisodeMappingList(0).episodeNum)
                _programIdentified = True
                TvSeriesEpisode = _Episode

                TvSeriesEpisode.ExistLocal = IdentifySeries.UpdateProgramAndTvMovieProgram(program, TvSeries, _Episode, _Episode.ExistLocal, True)

                MyLog.Info("enrichEPG: [IdentifySeries] [EpisodeIdentifed]: TvMovieEpisodeMapping: S{0}E{1} - {2} (newEpisode: {3})", _
                                      _Episode.SeriesNum, _Episode.EpisodeNum, program.EpisodeName, Not TvSeriesEpisode.ExistLocal)

                Return True
            End If
        End If

        '---------------- Nicht gefunden --------------------------------
        If _programIdentified = False Then
            'Nicht gefunden -> local = flase
            'Dim tmp As Boolean = IdentifySeries.UpdateProgramAndTvMovieProgram(program, TvSeries, Nothing, False, False)
            Dim _NotEpisode As MyTvSeries.MyEpisode = New MyTvSeries.MyEpisode
            _NotEpisode.ExistLocal = False
            TvSeriesEpisode = _NotEpisode

            TvSeriesEpisode.ExistLocal = IdentifySeries.UpdateProgramAndTvMovieProgram(program, TvSeries, _NotEpisode, False, False)

            'log ausgabe, falls disabled
            Dim _disabled As String = String.Empty
            Try
                Dim _SeriesMapping As TvMovieSeriesMapping = TvMovieSeriesMapping.Retrieve(TvSeries.idSeries)
                If _SeriesMapping.disabled = True Then
                    _disabled = ", Series is disabled -> "
                End If
            Catch ex As Exception
            End Try

            MyLog.Warn("enrichEPG: [IdentifySeries] [EpisodeIdentifed]: Not identified: {0} ({1}, {2}newEpisode: {3})", _
                               program.EpisodeName, program.ReferencedChannel.DisplayName, _disabled, Not TvSeriesEpisode.ExistLocal)
            Return False
        End If

    End Function

    Public Shared Function TheTvDbEpisodeIdentifed(ByVal program As Program) As Boolean
        Try

            Dim EpgEpisodeName As String = ReplaceSearchingString(program.EpisodeName)

            If SeriesLang.Episodes.Count > 0 Then
                Dim _episodeList As List(Of TvdbLib.Data.TvdbEpisode) = SeriesLang.Episodes.FindAll(Function(x) _
                        String.Compare(ReplaceSearchingString(x.EpisodeName), EpgEpisodeName, True) = 0)

                If _episodeList.Count > 0 Then
                    TheTvDbEpisode = _episodeList.Item(0)
                    logLevenstein = String.Empty
                    Return True
                Else
                    _episodeList = SeriesLang.Episodes.FindAll(Function(x) _
                        levenshtein(ReplaceSearchingString(x.EpisodeName), EpgEpisodeName) <= 2)

                    If _episodeList.Count > 0 Then
                        TheTvDbEpisode = _episodeList.Item(0)
                        logLevenstein = String.Format(", levenshtein: {0}", TheTvDbEpisode.EpisodeName)
                        Return True
                    Else
                        logLevenstein = String.Empty
                    End If
                End If
            End If

            'falls nicht gefunden noch auf englisch TheTvDb.com suchen
            If SeriesEN.Episodes.Count > 0 Then
                Dim _episodeList As List(Of TvdbLib.Data.TvdbEpisode) = SeriesEN.Episodes.FindAll(Function(x) _
                        String.Compare(ReplaceSearchingString(x.EpisodeName), EpgEpisodeName, True) = 0)

                If _episodeList.Count > 0 Then
                    TheTvDbEpisode = _episodeList.Item(0)
                    logLevenstein = String.Empty
                    Return True
                Else
                    _episodeList = SeriesEN.Episodes.FindAll(Function(x) _
                        levenshtein(ReplaceSearchingString(x.EpisodeName), EpgEpisodeName) <= 2)

                    If _episodeList.Count > 0 Then
                        TheTvDbEpisode = _episodeList.Item(0)
                        logLevenstein = String.Format(", levenshtein: {0}", TheTvDbEpisode.EpisodeName)
                        Return True
                    Else
                        logLevenstein = String.Empty
                    End If
                End If
            End If

            Return False

        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [IdentifySeries] [TheTvDbEpisodeIdentify]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try

    End Function

    Public Shared Function ReplaceSearchingString(ByVal expression As String) As String
        Return System.Text.RegularExpressions.Regex.Replace(expression, "[\:?,.!'-*()_]", "")
    End Function
    Public Shared Function levenshtein(ByVal a As [String], ByVal b As [String]) As Int32

        If String.IsNullOrEmpty(a) Then
            If Not String.IsNullOrEmpty(b) Then
                Return b.Length
            End If
            Return 0
        End If

        If String.IsNullOrEmpty(b) Then
            If Not String.IsNullOrEmpty(a) Then
                Return a.Length
            End If
            Return 0
        End If

        Dim cost As Int32
        Dim d As Int32(,) = New Integer(a.Length, b.Length) {}
        Dim min1 As Int32
        Dim min2 As Int32
        Dim min3 As Int32

        For i As Int32 = 0 To d.GetUpperBound(0)
            d(i, 0) = i
        Next

        For i As Int32 = 0 To d.GetUpperBound(1)
            d(0, i) = i
        Next

        For i As Int32 = 1 To d.GetUpperBound(0)
            For j As Int32 = 1 To d.GetUpperBound(1)
                cost = Convert.ToInt32(Not (a(i - 1) = b(j - 1)))

                min1 = d(i - 1, j) + 1
                min2 = d(i, j - 1) + 1
                min3 = d(i - 1, j - 1) + cost
                d(i, j) = Math.Min(Math.Min(min1, min2), min3)
            Next
        Next

        Return d(d.GetUpperBound(0), d.GetUpperBound(1))

    End Function
#End Region

    Public Class TheTvDb
#Region "Properties"

        Private Shared _idSeries As Integer = 0
        Public Shared ReadOnly Property IdSeries() As Integer
            Get
                Return _idSeries
            End Get
        End Property

        Private Shared _SeriesFound As Boolean = False
        Public Shared ReadOnly Property SeriesFound() As Boolean
            Get
                Return _SeriesFound
            End Get
        End Property

        Private Shared _SeriesFoundLanguage As String = String.Empty
        Public Shared ReadOnly Property SeriesFoundLanguage() As String
            Get
                Return _SeriesFoundLanguage
            End Get
        End Property

        Private Shared _PosterImageStatus As String = String.Empty
        Public Shared ReadOnly Property PosterImageStatus() As String
            Get
                Return _PosterImageStatus
            End Get
        End Property

        Private Shared _FanArtImageStatus As String = String.Empty
        Public Shared ReadOnly Property FanArtImageStatus() As String
            Get
                Return _FanArtImageStatus
            End Get
        End Property

        Private Shared _EpisodeImageStatus As String = String.Empty
        Public Shared ReadOnly Property EpisodeImageStatus() As String
            Get
                Return _EpisodeImageStatus
            End Get
        End Property

        Private Shared _EpisodeFound As Boolean = False
        Public Shared ReadOnly Property EpisodeFound() As Boolean
            Get
                Return _EpisodeFound
            End Get
        End Property

        Private Shared _idEpisode As String = String.Empty
        Public Shared ReadOnly Property IdEpisode() As String
            Get
                Return _idEpisode
            End Get
        End Property

        Private Shared _SeriesPosterPath As String = String.Empty
        Public Shared ReadOnly Property SeriesPosterPath() As String
            Get
                Return _SeriesPosterPath
            End Get
        End Property

        Private Shared _FanArtPath As String = String.Empty
        Public Shared ReadOnly Property FanArtPath() As String
            Get
                Return _FanArtPath
            End Get
        End Property

        Private Shared _EpisodeImagePath As String = String.Empty
        Public Shared ReadOnly Property EpisodeImagePath() As String
            Get
                Return _EpisodeImagePath
            End Get
        End Property

#End Region

        Public Shared Sub SearchSeries(ByVal SeriesName As String)
            Try
                'SerienId auf TheTvDB (_lang) suchen mit SerienName
                Dim _SearchSeriesResult As New List(Of TvdbLib.Data.TvdbSearchResult)
                _SearchSeriesResult = EnrichEPG.MyTVDBlang.TheTVdbHandler.SearchSeries(SeriesName)

                'Serien auf TheTvDB gefunden
                If _SearchSeriesResult.Count > 0 Then
                    For i = 0 To _SearchSeriesResult.Count - 1
                        Dim EpgSeriesName As String = ReplaceSearchingString(UCase(SeriesName))
                        Dim TheTvDBSeriesName As String = ReplaceSearchingString(UCase(_SearchSeriesResult(i).SeriesName))
                        If InStr(TheTvDBSeriesName, EpgSeriesName) > 0 Then
                            'SerienName gefunden, Episoden laden

                            'MyLog.Debug("[SearchSeries]: found lang")
                            _idSeries = _SearchSeriesResult(i).Id
                            _SeriesFound = True

                            'episoden laden lang | en
                            IdentifySeries.SeriesLang = EnrichEPG.MyTVDBlang.TheTVdbHandler.GetSeries(_idSeries, EnrichEPG.MyTVDBlang.DBLanguage, True, False, False)
                            IdentifySeries.SeriesEN = EnrichEPG.MyTVDBen.TheTVdbHandler.GetSeries(_idSeries, EnrichEPG.MyTVDBen.DBLanguage, True, False, False)

                            _SeriesFoundLanguage = EnrichEPG.MyTVDBlang.tvLanguage

                            Exit For
                        Else
                            'SerienName nicht auf TheTvDB gefunden
                            'MyLog.Debug("[SearchSeries]: SeriesName not found lang")
                            _idSeries = 0
                            _SeriesFound = False
                        End If
                    Next
                Else
                    'keine Serien auf TheTvDB gefunden
                    'MyLog.Debug("[SearchSeries]: Series not found lang")
                    _idSeries = 0
                    _SeriesFound = False
                End If

                'Sofern keine Serie gefunden, SerienId auf TheTvDB (en) suchen mit SerienName
                If _SeriesFound = False Then
                    _SearchSeriesResult.Clear()
                    _SearchSeriesResult = EnrichEPG.MyTVDBen.TheTVdbHandler.SearchSeries(SeriesName)

                    'Serien auf TheTvDB gefunden
                    If _SearchSeriesResult.Count > 0 Then
                        For d = 0 To _SearchSeriesResult.Count - 1
                            Dim EpgSeriesName As String = ReplaceSearchingString(UCase(SeriesName))
                            Dim TheTvDBSeriesName As String = ReplaceSearchingString(UCase(_SearchSeriesResult(d).SeriesName))
                            If InStr(TheTvDBSeriesName, EpgSeriesName) > 0 Then
                                'SerienName gefunden, Episoden laden

                                'MyLog.Debug("[SearchSeries]: found en")
                                _idSeries = _SearchSeriesResult(d).Id
                                _SeriesFound = True

                                'episoden laden lang | en
                                IdentifySeries.SeriesLang = EnrichEPG.MyTVDBlang.TheTVdbHandler.GetSeries(_idSeries, EnrichEPG.MyTVDBlang.DBLanguage, True, False, False)
                                IdentifySeries.SeriesEN = EnrichEPG.MyTVDBen.TheTVdbHandler.GetSeries(_idSeries, EnrichEPG.MyTVDBen.DBLanguage, True, False, False)

                                _SeriesFoundLanguage = EnrichEPG.MyTVDBen.tvLanguage

                                Exit For
                            Else
                                'SerienName nicht auf TheTvDB gefunden
                                'MyLog.Debug("[SearchSeries]: SeriesName not found en")
                                _idSeries = 0
                                _SeriesFound = False
                            End If
                        Next
                    Else
                        'Series nicht auf TheTvDB gefunden
                        'MyLog.Debug("[SearchSeries]: Series not found en")
                        _idSeries = 0
                        _SeriesFound = False
                    End If
                End If

                'Wenn Serie nicht gefunden, prüfen ob ": " oder " - " im SerienNamen, alles danach abschneiden -> neu suchen
                If _SeriesFound = False Then
                    If InStr(SeriesName, " - ") > 0 Then
                        SeriesName = Left(SeriesName, InStr(SeriesName, " - ") - 1)
                        MyLog.Info("enrichEPG: [SearchSeries]: not found on TheTvDb.com -> Try with SeriesName: {0}", SeriesName)
                        SearchSeries(SeriesName)
                    End If
                    If InStr(SeriesName, ": ") > 0 Then
                        SeriesName = Left(SeriesName, InStr(SeriesName, ": ") - 1)
                        MyLog.Info("enrichEPG: [SearchSeries]: not found on TheTvDb.com -> Try with SeriesName: {0}", SeriesName)
                        SearchSeries(SeriesName)
                    End If
                End If

            Catch ex As Exception
                _idSeries = 0
                _SeriesFound = False
                MyLog.[Error]("enrichEPG: [IdentifySeries] [GetSeriesID]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
            End Try
        End Sub
        Public Shared Sub LoadCoverAndFanart()
            Try
                'SeriesPoster & Fanart herunterladen wenn Pfad vorhanden
                If String.IsNullOrEmpty(MySettings.MpThumbPath) = False Then

                    If _SeriesFound = True Then
                        Try


                            'SeriesPoster laden 
                            If IdentifySeries.SeriesEN.PosterBanners.Count > 0 Then
                                _SeriesPosterPath = "Clickfinder ProgramGuide\" & _idSeries & "\" & _idSeries & "_poster.jpg"

                                If File.Exists(MySettings.MpThumbPath & "\MPTVSeriesBanners\" & _SeriesPosterPath) = False Then
                                    IdentifySeries.SeriesEN.PosterBanners(0).LoadThumb()
                                    IO.Directory.CreateDirectory(MySettings.MpThumbPath & "\MPTVSeriesBanners\Clickfinder ProgramGuide\" & _idSeries)
                                    IdentifySeries.SeriesEN.PosterBanners(0).ThumbImage.Save(MySettings.MpThumbPath & "\MPTVSeriesBanners\" & _SeriesPosterPath)
                                    _PosterImageStatus = "downloaded"
                                    IdentifySeries.SeriesEN.PosterBanners(0).UnloadThumb()
                                Else
                                    _PosterImageStatus = "exists"
                                End If
                            Else
                                _PosterImageStatus = "nothing found"
                            End If
                        Catch ex As Exception
                            _PosterImageStatus = "error"
                            MyLog.[Error]("enrichEPG: [IdentifySeries] [LoadCoverAndFanart]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
                        End Try

                        Try
                            'FanArt laden
                            If IdentifySeries.SeriesEN.FanartBanners.Count > 0 Then
                                _FanArtPath = "Fan Art\Clickfinder ProgramGuide\" & _idSeries & "\" & _idSeries & "_fanArt.jpg"

                                If File.Exists(MySettings.MpThumbPath & "\" & _FanArtPath) = False Then
                                    IdentifySeries.SeriesEN.FanartBanners(0).LoadBanner()
                                    IO.Directory.CreateDirectory(MySettings.MpThumbPath & "\Fan Art\Clickfinder ProgramGuide\" & _idSeries)
                                    IdentifySeries.SeriesEN.FanartBanners(0).BannerImage.Save(MySettings.MpThumbPath & "\" & _FanArtPath)
                                    _FanArtImageStatus = "downloaded"
                                    IdentifySeries.SeriesEN.FanartBanners(0).UnloadBanner()
                                Else
                                    _FanArtImageStatus = "exists"
                                End If
                            Else
                                _FanArtImageStatus = "nothing found"
                            End If
                        Catch ex As Exception
                            _FanArtImageStatus = "error"
                            MyLog.[Error]("enrichEPG: [IdentifySeries] [LoadCoverAndFanart]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
                        End Try
                    End If
                End If
            Catch ex As Exception
                MyLog.[Error]("enrichEPG: [IdentifySeries] [LoadCoverAndFanart]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
            End Try
        End Sub

        Public Shared Sub SearchEpisode(ByVal SeasonNum As Integer, ByVal EpisodeNum As Integer)
            Try
                'Nur ausführen wenn Series davor gefunden
                If _SeriesFound = True Then
                    If IdentifySeries.SeriesEN.Episodes.Count > 0 Then
                        For z = 0 To IdentifySeries.SeriesEN.Episodes.Count - 1
                            'Episode über SeasonNum & EpisodeNum identifizieren
                            If SeasonNum = IdentifySeries.SeriesEN.Episodes(z).SeasonNumber And EpisodeNum = IdentifySeries.SeriesEN.Episodes(z).EpisodeNumber Then
                                _EpisodeFound = True
                                TheTvDbEpisode = IdentifySeries.SeriesEN.Episodes(z)
                                _idEpisode = _idSeries & "_" & TheTvDbEpisode.SeasonNumber & "x" & TheTvDbEpisode.EpisodeNumber

                                Exit For
                            Else
                                'Episode nicht identifiziert
                                _idEpisode = String.Empty
                                _EpisodeFound = False
                            End If
                        Next
                    Else
                        'Serie hat keine Episoden
                        _idEpisode = String.Empty
                        _EpisodeFound = False
                    End If
                Else
                    'Keine Serie gefunden
                    _idEpisode = String.Empty
                    _EpisodeFound = False
                End If

            Catch ex As Exception
                _idEpisode = String.Empty
                _EpisodeFound = False
                MyLog.[Error]("enrichEPG: [IdentifySeries] [SearchEpisode]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
            End Try
        End Sub
        Public Shared Sub LoadEpisodeImage()
            Try
                'Episode Image herunterladen wenn Pfad vorhanden
                If String.IsNullOrEmpty(MySettings.MpThumbPath) = False Then

                    If _EpisodeFound = True Then
                        'EpisodeImage laden

                        'EpisodenImage laden
                        _EpisodeImagePath = "Clickfinder ProgramGuide\" & _idSeries & "\" & _idSeries & "_" & TheTvDbEpisode.SeasonNumber & "x" & TheTvDbEpisode.EpisodeNumber & ".jpg"

                        If Not File.Exists(MySettings.MpThumbPath & "\MPTVSeriesBanners\" & _EpisodeImagePath) Then
                            TheTvDbEpisode.Banner.LoadThumb()
                            IO.Directory.CreateDirectory(MySettings.MpThumbPath & "\MPTVSeriesBanners\Clickfinder ProgramGuide\" & _idSeries)
                            TheTvDbEpisode.Banner.ThumbImage.Save(MySettings.MpThumbPath & "\MPTVSeriesBanners\" & _EpisodeImagePath)
                            _EpisodeImageStatus = "downloaded"
                            TheTvDbEpisode.Banner.UnloadThumb()
                        Else
                            _EpisodeImageStatus = "exists"
                        End If

                    End If

                End If
            Catch ex As Exception
                MyLog.[Error]("enrichEPG: [IdentifySeries] [LoadEpisodeImage]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
            End Try
        End Sub

        Public Shared Sub ResetCoverAndFanartPath()
            _SeriesPosterPath = String.Empty
            _FanArtPath = String.Empty
        End Sub
        Public Shared Sub ResetEpisodeImagePath()
            _EpisodeImagePath = String.Empty
        End Sub
    End Class

End Class
