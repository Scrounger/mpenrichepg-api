﻿Imports enrichEPG.TvDatabase
Imports TvDatabase
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

Public Class IdentifySeries

#Region "Members"
    Private Shared _SeriesEN As TvdbLib.Data.TvdbSeries
    Private Shared _SeriesLang As TvdbLib.Data.TvdbSeries
    Private Shared _IdentifiedEpisode As TvdbLib.Data.TvdbEpisode
    Private Shared _UpdateEpgEpisodeSeriesNameCounter As Integer
    Private Shared _idSeries
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
    Public Shared Property IdentifiedEpisode() As TvdbLib.Data.TvdbEpisode
        Get
            Return _IdentifiedEpisode
        End Get
        Set(ByVal value As TvdbLib.Data.TvdbEpisode)
            _IdentifiedEpisode = value
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
#End Region

#Region "Functions"
    Public Shared Sub UpdateEpgEpisode(ByVal program As Program, ByVal TvSeriesDB As TVSeriesDB, ByVal TvSeriesDBname As String)
        Try

            'Daten im EPG (program) updaten
            program.SeriesNum = CStr(TvSeriesDB.SeasonIndex)
            program.EpisodeNum = CStr(TvSeriesDB.EpisodeIndex)
            'keine Exception, weil alternativ Series Rating
            program.StarRating = TvSeriesDB.EpisodeRating
            program.Persist()

            UpdateEpgEpisodeSeriesName(program, TvSeriesDBname)

        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [IdentifySeries] [UpdateEpgEpisode]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try
    End Sub
    Private Shared Sub UpdateEpgEpisodeSeriesName(ByVal program As Program, ByVal TvSeriesDBname As String)
        Try
            'Daten im EPG (program) updaten
            If Not TvSeriesDBname = program.Title Then
                Dim tmpProgramTitle As String = program.Title
                program.Title = TvSeriesDBname
                program.Persist()

                UpdateEpgEpisodeSeriesNameCounter = UpdateEpgEpisodeSeriesNameCounter + 1
            End If

        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [IdentifySeries] [UpdateEpgEpisodeSeriesName]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try
    End Sub
    Public Shared Sub MarkEpgEpisodeAsNew(ByVal program As Program, ByVal EpisodeExistsLocal As Boolean)
        Try
            Select Case EnrichEPG.ClickfinderProgramGuideImportEnable
                Case Is = False
                    'ohne Clickfinder
                    If EpisodeExistsLocal = False Then
                        If InStr(program.Description, EnrichEPG.NewEpisodeString) = 0 Then
                            program.Description = EnrichEPG.NewEpisodeString & vbNewLine & program.Description
                        ElseIf InStr(program.Description, EnrichEPG.EpisodeExistsString) > 0 Then
                            program.Description = Replace(program.Description, EnrichEPG.EpisodeExistsString, EnrichEPG.NewEpisodeString)
                        End If
                    Else
                        If InStr(program.Description, EnrichEPG.EpisodeExistsString) = 0 Then
                            program.Description = EnrichEPG.EpisodeExistsString & vbNewLine & program.Description
                        ElseIf InStr(program.Description, EnrichEPG.NewEpisodeString) > 0 Then
                            program.Description = Replace(program.Description, EnrichEPG.NewEpisodeString, EnrichEPG.EpisodeExistsString)
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
    Public Shared Sub UpdateTvMovieProgram(ByVal program As Program, ByVal TvSeriesDB As TVSeriesDB, ByVal indexTvSeriesDB As Integer, ByVal EpisodeExistsLocal As Boolean)
        Try
            If EnrichEPG.ClickfinderProgramGuideImportEnable = True Then
                'Zunächst nur Serien Infos schreiben (episode in TvSeriesDB nicht gefunden)

                'idProgram in TvMovieProgram suchen & Daten aktualisieren
                Dim _TvMovieProgram As TVMovieProgram = Helper.getTvMovieProgram(program.IdProgram)
                _TvMovieProgram.idSeries = TvSeriesDB(indexTvSeriesDB).SeriesID
                'erstmal auf local
                _TvMovieProgram.local = EpisodeExistsLocal
                _TvMovieProgram.TVMovieBewertung = 6

                'Serien Poster Image
                If Not String.IsNullOrEmpty(TvSeriesDB(indexTvSeriesDB).SeriesPosterImage) = True Then
                    _TvMovieProgram.SeriesPosterImage = TvSeriesDB(indexTvSeriesDB).SeriesPosterImage
                End If

                'FanArt
                If Not String.IsNullOrEmpty(TvSeriesDB(indexTvSeriesDB).FanArt) = True Then
                    _TvMovieProgram.FanArt = TvSeriesDB(indexTvSeriesDB).FanArt
                End If

                'Episoden Infos schreiben -> in TvSeries DB gefunden
                If EpisodeExistsLocal = True Then

                    _TvMovieProgram.idEpisode = TvSeriesDB.EpisodeCompositeID
                    _TvMovieProgram.FileName = TvSeriesDB.EpisodeFilename

                    'Episoden Image
                    If Not String.IsNullOrEmpty(TvSeriesDB.EpisodeImage) = True Then
                        _TvMovieProgram.EpisodeImage = TvSeriesDB.EpisodeImage
                    End If
                End If

                'MyLog.debug("{0}, {1},idseries: {2},idepisode: {3}, local: {4}", program.Title, program.EpisodeName, _TvMovieProgram.idSeries, _TvMovieProgram.idEpisode, _TvMovieProgram.local)

                _TvMovieProgram.Persist()

            End If

        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [IdentifySeries] [UpdateTvMovieProgram]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try
    End Sub
    Public Shared Function UpdateProgramAndTvMovieProgram(ByVal program As Program, ByVal TvSeriesDB As TVSeriesDB, ByVal indexTvSeriesDB As Integer, ByVal EpisodeExistLocal As Boolean) As Boolean
        Try
            'False: lokal = 0 
            'True: local = 1
            Dim _Local As Boolean = EpisodeExistLocal

            'disabled = existiert
            If TVMovieProgram.Retrieve(program.IdProgram).SeriesDisabled = True Then
                _Local = True
            End If

            'Daten im EPG (program) updaten
            IdentifySeries.UpdateEpgEpisode(program, TvSeriesDB, TvSeriesDB(indexTvSeriesDB).SeriesName)

            'Neue Episode -> im EPG Describtion kennzeichnen
            IdentifySeries.MarkEpgEpisodeAsNew(program, _Local)

            'Clickfinder ProgramGuide Infos in TvMovieProgram schreiben, sofern aktiviert
            IdentifySeries.UpdateTvMovieProgram(program, TvSeriesDB, indexTvSeriesDB, _Local)

            Return _Local

        Catch ex As Exception
            MyLog.[Error]("enrichEPG: [UpdateProgramAndTvMovieProgram] [UpdateTvMovieProgram]: exception err:{0} stack:{1}", ex.Message, ex.StackTrace)
        End Try
    End Function
    Public Shared Function TheTvDbEpisodeIdentify(ByVal program As Program) As Boolean
        Try

            Dim EpgEpisodeName As String = ReplaceSearchingString(UCase(program.EpisodeName))

            If SeriesLang.Episodes.Count > 0 Then
                For y = 0 To SeriesLang.Episodes.Count - 1
                    Dim TheTvDbEpisodeName As String = ReplaceSearchingString(UCase(SeriesLang.Episodes(y).EpisodeName))

                    If TheTvDbEpisodeName = EpgEpisodeName Then
                        IdentifiedEpisode = SeriesLang.Episodes(y)
                        Return True
                    Else
                        Dim _variance As Integer = levenshtein(TheTvDbEpisodeName, EpgEpisodeName)

                        If _variance <= 2 Then

                            MyLog.Debug("enrichEPG: [IdentifySeries] [TheTvDbEpisodeIdentify]: levenshtein variance = {0} (EPG: {1}, TheTvDb: {2})", _variance, program.EpisodeName, SeriesLang.Episodes(y).EpisodeName)
                            IdentifiedEpisode = SeriesLang.Episodes(y)
                            Return True
                        End If
                    End If
                Next
            End If

            'falls nicht gefunden noch auf englisch TheTvDb.com suchen
            If SeriesEN.Episodes.Count > 0 Then
                For z = 0 To SeriesEN.Episodes.Count - 1
                    Dim TheTvDbEpisodeName As String = ReplaceSearchingString(UCase(SeriesEN.Episodes(z).EpisodeName))

                    If TheTvDbEpisodeName = EpgEpisodeName Then
                        IdentifiedEpisode = SeriesEN.Episodes(z)
                        Return True
                    Else
                        Dim _variance As Integer = levenshtein(TheTvDbEpisodeName, EpgEpisodeName)

                        If _variance <= 2 Then

                            MyLog.Debug("enrichEPG: [IdentifySeries] [TheTvDbEpisodeIdentify]: levenshtein variance = {0} (EPG: {1}, TheTvDb: {2})", _variance, program.EpisodeName, SeriesEN.Episodes(z).EpisodeName)
                            IdentifiedEpisode = SeriesEN.Episodes(z)
                            Return True
                        End If
                    End If
                Next
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
                If String.IsNullOrEmpty(EnrichEPG.MpThumbPath) = False Then

                    If _SeriesFound = True Then

                        'SeriesPoster laden 
                        If IdentifySeries.SeriesEN.PosterBanners.Count > 0 Then
                            _SeriesPosterPath = "Clickfinder ProgramGuide\" & _idSeries & "\" & _idSeries & "_poster.jpg"

                            If File.Exists(EnrichEPG.MpThumbPath & "\MPTVSeriesBanners\" & _SeriesPosterPath) = False Then
                                IdentifySeries.SeriesEN.PosterBanners(0).LoadThumb()
                                IO.Directory.CreateDirectory(EnrichEPG.MpThumbPath & "\MPTVSeriesBanners\Clickfinder ProgramGuide\" & _idSeries)
                                IdentifySeries.SeriesEN.PosterBanners(0).ThumbImage.Save(EnrichEPG.MpThumbPath & "\MPTVSeriesBanners\" & _SeriesPosterPath)
                                _PosterImageStatus = "downloaded"
                                IdentifySeries.SeriesEN.PosterBanners(0).UnloadThumb()
                            Else
                                _PosterImageStatus = "exists"
                            End If
                        Else
                            _PosterImageStatus = "nothing found"
                        End If

                        'FanArt laden
                        If IdentifySeries.SeriesEN.FanartBanners.Count > 0 Then
                            _FanArtPath = "Fan Art\Clickfinder ProgramGuide\" & _idSeries & "\" & _idSeries & "_fanArt.jpg"

                            If File.Exists(EnrichEPG.MpThumbPath & "\" & _FanArtPath) = False Then
                                IdentifySeries.SeriesEN.FanartBanners(0).LoadBanner()
                                IO.Directory.CreateDirectory(EnrichEPG.MpThumbPath & "\Fan Art\Clickfinder ProgramGuide\" & _idSeries)
                                IdentifySeries.SeriesEN.FanartBanners(0).BannerImage.Save(EnrichEPG.MpThumbPath & "\" & _FanArtPath)
                                _FanArtImageStatus = "downloaded"
                                IdentifySeries.SeriesEN.FanartBanners(0).UnloadBanner()
                            Else
                                _FanArtImageStatus = "exists"
                            End If
                        Else
                            _FanArtImageStatus = "nothing found"
                        End If

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
                                IdentifiedEpisode = IdentifySeries.SeriesEN.Episodes(z)
                                _idEpisode = _idSeries & "_" & IdentifiedEpisode.SeasonNumber & "x" & IdentifiedEpisode.EpisodeNumber

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
                If String.IsNullOrEmpty(EnrichEPG.MpThumbPath) = False Then

                    If _EpisodeFound = True Then
                        'EpisodeImage laden

                        'EpisodenImage laden
                        _EpisodeImagePath = "Clickfinder ProgramGuide\" & _idSeries & "\" & _idSeries & "_" & IdentifiedEpisode.SeasonNumber & "x" & IdentifiedEpisode.EpisodeNumber & ".jpg"

                        If Not File.Exists(EnrichEPG.MpThumbPath & "\MPTVSeriesBanners\" & _EpisodeImagePath) Then
                            IdentifiedEpisode.Banner.LoadThumb()
                            IO.Directory.CreateDirectory(EnrichEPG.MpThumbPath & "\MPTVSeriesBanners\Clickfinder ProgramGuide\" & _idSeries)
                            IdentifiedEpisode.Banner.ThumbImage.Save(EnrichEPG.MpThumbPath & "\MPTVSeriesBanners\" & _EpisodeImagePath)
                            _EpisodeImageStatus = "downloaded"
                            IdentifiedEpisode.Banner.UnloadThumb()
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
