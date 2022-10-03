Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel
Imports System.Data.Odbc
Imports System.Data.OleDb
Imports System.Data
Imports System.Security.Cryptography
Imports System.IO
Imports System.Text
Imports System.Threading
Imports Microsoft.Win32
Imports System.Globalization


' Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la siguiente línea.
' <System.Web.Script.Services.ScriptService()> _
<System.Web.Services.WebService(Namespace:="http://tempuri.org/")> _
<System.Web.Services.WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<ToolboxItem(False)> _
Public Class Servicio_Llenos
    Inherits System.Web.Services.WebService


    Dim ds_ValidVisitItems As New Data.DataSet()
    Dim adp_ValidVisitItems_adapter As New Data.OleDb.OleDbDataAdapter()


    Dim oleDBconnx As OleDbConnection
    Dim oleDBcom As OleDbCommand

    'Contructor del Servicio Web
    Public Sub New()
        oleDBcom = New OleDbCommand()
        oleDBconnx = New OleDbConnection()
        Dim strconx As String
        strconx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        oleDBconnx.ConnectionString = strconx
        oleDBcom = oleDBconnx.CreateCommand
    End Sub


    <WebMethod()> _
   Public Function Login(ByVal usuario As String, ByVal password As String) As DataTable
        'Dim usuario = "ricardo"
        'Dim password = "E10ADC3949BA59ABBE56E057F20F883E"

        Dim oleDBconnx As OleDbConnection
        Dim oleDBcom As OleDbCommand
        oleDBcom = New OleDbCommand()
        oleDBconnx = New OleDbConnection()
        Dim strconx As String
        strconx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        oleDBconnx.ConnectionString = strconx
        oleDBcom = oleDBconnx.CreateCommand

        '----------------------------------
        Dim _ODBPar_UserId As New OleDbParameter("@intUserId", OleDbType.Integer)
        Dim _ODBPar_UserName As New OleDbParameter("@strUserName", OleDbType.VarChar)
        Dim _ODBPar_AuthorizationPassword As New OleDbParameter("@strUserAuthorizationPassword", OleDbType.VarChar)
        Dim _ODBPar_UserPsw As New OleDbParameter("@strUserPassword", OleDbType.VarChar)
        Dim _ODBPar_Active As New OleDbParameter("@blnUserActive", OleDbType.Integer)
        Dim ls_SQL_Command As String

        Dim DataResult As DataTable = New Data.DataTable() 'DataSet = New DataSet()
        Dim adapter As OleDbDataAdapter

        Dim llng_Timeout As Long = 0
        Dim lint_tries As Integer = 0
        Dim lint_counterTrys As Integer = 0
        Dim lint_HasError As Integer = 0
        Dim lint_IsTimeError As Integer = 0

        ' obtener el  valor de time out 
        llng_Timeout = of_getTimeoutSearch()
        ' obteneer la cantida de intentos 
        lint_tries = of_getSearchTries()


        '' ciclo
        Do

            ' reinicializacion de variables 
            oleDBcom = New OleDbCommand()
            oleDBconnx = New OleDbConnection()
            oleDBconnx.ConnectionString = strconx
            oleDBcom = oleDBconnx.CreateCommand
            ''
            _ODBPar_UserId = New OleDbParameter("@intUserId", OleDbType.Integer)
            _ODBPar_UserName = New OleDbParameter("@strUserName", OleDbType.VarChar)
            _ODBPar_AuthorizationPassword = New OleDbParameter("@strUserAuthorizationPassword", OleDbType.VarChar)
            _ODBPar_UserPsw = New OleDbParameter("@strUserPassword", OleDbType.VarChar)
            _ODBPar_Active = New OleDbParameter("@blnUserActive", OleDbType.Integer)
            ls_SQL_Command = ""

            lint_HasError = 0
            lint_IsTimeError = 0

            'redefinicion de parametros

            _ODBPar_UserId.Value = 0
            _ODBPar_UserName.Value = usuario
            _ODBPar_UserPsw.Value = password
            _ODBPar_AuthorizationPassword.Value = ""
            _ODBPar_Active.Value = 1

            ls_SQL_Command = "spFindtblclsUser"

            ' asociacion de parametros al comando

            oleDBcom.Parameters.Add(_ODBPar_UserId)
            oleDBcom.Parameters.Add(_ODBPar_UserName)
            oleDBcom.Parameters.Add(_ODBPar_UserPsw)
            oleDBcom.Parameters.Add(_ODBPar_AuthorizationPassword)
            oleDBcom.Parameters.Add(_ODBPar_Active)

            oleDBcom.CommandText = ls_SQL_Command
            oleDBcom.CommandType = CommandType.StoredProcedure

            DataResult = New Data.DataTable() 'DataSet = New DataSet()
            DataResult.TableName = "TrearDatos"
            adapter = New OleDbDataAdapter(oleDBcom)

            ' si hay timeout, del archivo ponerlo 
            If llng_Timeout > 0 Then
                oleDBcom.CommandTimeout = llng_Timeout
            Else
                oleDBcom.CommandTimeout = 0
            End If

            Try
                oleDBconnx.Open()
                'oleDBcom.ExecuteNonQuery()
                adapter.Fill(DataResult)
                lint_HasError = 0

                'si llego hasta aqui no hubo timeout
                lint_counterTrys = lint_tries

            Catch ex As Exception
                Dim strError As String
                strError = ObtenerError(ex.Message, 99999)

                If strError.ToLower.Contains("excedio") = True Or strError.ToLower.Contains("tiempo") = True Or strError.ToLower.Contains("time") = True Then
                    lint_IsTimeError = 1
                Else
                    lint_IsTimeError = 0
                    'hubo un error no era de timeout. ya forzar el ciclos
                    lint_counterTrys = lint_tries

                End If

                DataResult = dt_RetrieveErrorTable(strError)

            Finally
                oleDBconnx.Close()
                oleDBconnx.Dispose()
                oleDBconnx = Nothing

            End Try


            lint_counterTrys = lint_counterTrys + 1
            ' mientras el contador sea menor que el limite del archivo 

        Loop While lint_counterTrys < lint_tries


        Return DataResult

    End Function

    ''''''''''''
    '' agregar el meotode de busqueda 
    '''''''''''''
    ''''''''''''
    <WebMethod()> _
   Public Function Buscar_Datos_ContenedorLleno(ByVal strcontainerid As String) As DataTable
        Dim idt_result As DataTable = New DataTable ' Tabla con el query de resultados 
        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando
        Dim iolecmd_comand As OleDbCommand '' objeto comando que se ejecutara
        Dim ioleconx_conexion As OleDbConnection = New OleDbConnection() '' objeto de conexion que se usara para conectar 
        Dim istr_conx As String '' cadena de conexion
        'Dim strcontainerid As String = "IPXU3283286"
        istr_conx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        ioleconx_conexion.ConnectionString = istr_conx
        iolecmd_comand = ioleconx_conexion.CreateCommand()
        idt_result.TableName = "Buscar_Datos"

        Dim strSQL As String
        'Dim strcontainerid As String

        Dim llng_Timeout As Long = 0
        Dim lint_tries As Integer = 0
        Dim lint_counterTrys As Integer = 0
        Dim lint_HasError As Integer = 0
        Dim lint_IsTimeError As Integer = 0

        ' obtener el  valor de time out 
        llng_Timeout = of_getTimeoutSearch()
        ' obteneer la cantida de intentos 
        lint_tries = of_getSearchTries()



        'Sentencia SQL que recobra los datos para la pantalla de cambio de Ubicacion
        strSQL = "SELECT  tblclsContainerInventory.strContainerId, " & _
                         "tblclsContainerInventory.intContainerUniversalId, " & _
                         "isnull(tblclsContainerInventory.decContainerInventoryWeight,0) as 'decContainerInventoryVGM', " & _
                         "tblclsContainerType.strContainerTypeIdentifier, " & _
                         "tblclsContainerSize.strContainerSizeIdentifier, " & _
                         "tblclsContainer.decContainerTare, " & _
                         "tblclsContainerInventory.decContainerInventoryWeight, " & _
                         "tblclsContainerInventory.blnContainerIsFull, " & _
                         "(CASE ISNULL(tblclsContainerInventory.intContainerUniversalId, 0) " & _
                               "WHEN 0 THEN 'SIN ESTATUS' " & _
                               "ELSE tblclsContainerFiscalStatus.strContFisStatusIdentifier " & _
                          "END) AS 'strContFisStatusIdentifier' , " & _
                         "tblclsContainerAdmStatus.strContAdmStatusIdentifier, " & _
                         "tblclsContainerInventory.strContainerInvYardPositionId, " & _
                         "tblclsContainerInventory.strContainerInvComments, " & _
                         "DATEDIFF(dd, dtmContainerInvReceptionDate, GETDATE()) As intDaysInTerminal ," & _
                         "  VSS.strVesselName, VVY.vchVesselVoyageDescription " & _
                         " , VSS.strVesselName +'-'+ CONVERT(VARCHAR(2),DATEPART(dd,VVY.dteVesselVoyageArrivalDate)) +'/'+CONVERT(VARCHAR(2),DATEPART(mm,VVY.dteVesselVoyageArrivalDate)) + '/'+CONVERT(VARCHAR(4),DATEPART(yy,VVY.dteVesselVoyageArrivalDate))  AS strVesselAndDate " & _
                         " ,VSS.strVesselIdentifier + '-' +VVY.strVesselVoyageNumIdentifier as strVesselIdandVoyageId" & _
                         " ,SHIP.strShippingLineIdentifier " & _
                         " ,tblclsContainerInventory.strContainerInvComments " & _
                         " , CASE WHEN LEN( tblclsContainerInventory.strContainerInvFinalPortId) > 1 THEN tblclsContainerInventory.strContainerInvFinalPortId " & _
                         "   ELSE tblclsContainerInventory.strContainerInvDischargePortId" & _
                         "  END   AS strFinalPort " & _
                         " ,FMOV.strFiscalMovementIdentifier " & _
                         " ,( SELECT ISNULL(MAX( tblclsContainerSeal.strContainerSealNumber ),'') " & _
                         "     FROM tblclsContainerSeal " & _
                         "     WHERE tblclsContainerSeal.intContainerUniversalId = tblclsContainerInventory.intContainerUniversalId ) AS SEAL " & _
                         " ,( SELECT ISNULL(MAX(tblclsIMOCode.strIMOCodeIdentifier),'') " & _
                         "    FROM tblIMOCode_ContainerInventory " & _
                         "       INNER JOIN tblclsIMOCode ON tblclsIMOCode.intIMOCodeId = tblIMOCode_ContainerInventory.intIMOCodeId " & _
                         "    WHERE tblIMOCode_ContainerInventory.intContainerUniversalId = tblclsContainerInventory.intContainerUniversalId ) AS IMO " & _
                         " FROM tblclsContainerInventory " & _
                         " LEFT JOIN tblclsContainerFiscalStatus " & _
                          " ON tblclsContainerInventory.intContFisStatusId = tblclsContainerFiscalStatus.intContFisStatusId " & _
                         " LEFT JOIN tblclsContainer " & _
                           "ON tblclsContainerInventory.strContainerId = tblclsContainer.strContainerId " & _
                         " LEFT JOIN tblclsContainerISOCode " & _
                          " ON tblclsContainer.intContISOCodeId = tblclsContainerISOCode.intContISOCodeId " & _
                         " LEFT JOIN tblclsContainerType " & _
                           " ON tblclsContainerISOCode.intContainerTypeId = tblclsContainerType.intContainerTypeId " & _
                         " LEFT JOIN tblclsContainerSize " & _
                           " ON tblclsContainerSize.intContainerSizeId  = tblclsContainerISOCode.intContainerSizeId " & _
                         " LEFT JOIN tblclsVesselVoyage VVY " & _
                           " ON  tblclsContainerInventory.intContainerInvVesselVoyageId = VVY.intVesselVoyageId  " & _
                         " LEFT JOIN tblclsVessel VSS " & _
                           "  ON VVY.intVesselId = VSS.intVesselId  " & _
                         " LEFT JOIN tblclsShippingLine SHIP " & _
                           " ON SHIP.intShippingLineId = tblclsContainerInventory.intContainerInvOperatorId  " & _
                         " LEFT JOIN tblclsFiscalMovement FMOV " & _
                           " ON FMOV.intFiscalMovementId  = tblclsContainerInventory.intFiscalMovementId " & _
                         " LEFT JOIN tblclsContainerAdmStatus " & _
                           " ON  tblclsContainerAdmStatus.intContAdmStatusId = tblclsContainerInventory.intContAdmStatusId " & _
                   " WHERE ( tblclsContainerInventory.strContainerId = tblclsContainer.strContainerId ) and " & _
                         " ( tblclsContainerInventory.blnContainerInvActive = 1 ) and " & _
                         " (tblclsContainerInventory.strContainerId = '" & strcontainerid & "') "


        'ciclo

        Do
            idt_result = New DataTable
            iAdapt_comand = New OleDbDataAdapter()
            ioleconx_conexion = New OleDbConnection() '' objeto de conexion que se usara para conectar 
            istr_conx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
            ioleconx_conexion.ConnectionString = istr_conx
            iolecmd_comand = ioleconx_conexion.CreateCommand()
            idt_result.TableName = "Buscar_Datos"

            ' consulta 

            iolecmd_comand.CommandText = strSQL

            iAdapt_comand.SelectCommand = iolecmd_comand

            If llng_Timeout > 0 Then
                oleDBcom.CommandTimeout = llng_Timeout
            Else
                oleDBcom.CommandTimeout = 0
            End If

            Try
                iolecmd_comand.Connection.Open()
                iAdapt_comand.Fill(idt_result)
                lint_HasError = 0

                'si llego hasta aqui no hubo timeout
                lint_counterTrys = lint_tries

            Catch ex As Exception
                Dim strError As String
                strError = ObtenerError(ex.Message, 99999)
                idt_result = dt_RetrieveErrorTable(strError)

                If strError.ToLower.Contains("excedio") = True Or strError.ToLower.Contains("tiempo") = True Or strError.ToLower.Contains("time") = True Then
                    lint_IsTimeError = 1
                Else
                    lint_IsTimeError = 0
                    'hubo un error no era de timeout. ya forzar el ciclos
                    lint_counterTrys = lint_tries
                End If

            Finally
                iAdapt_comand.SelectCommand.Connection.Close()
                iolecmd_comand.Connection.Close()
                iolecmd_comand.Connection.Dispose()
                iAdapt_comand.SelectCommand.Connection.Dispose()
                ioleconx_conexion.Close()
                ioleconx_conexion.Dispose()

            End Try

            '' -- si el el resultado obtenido es de un renglon validar que el puerto , si es cadena vacia 
            'If idt_result.Rows.Count = 1 Then
            '    If IsDBNull(idt_result(0)("strFinalPort")) = True Then
            '        idt_result(0)("strFinalPort") = ""
            '        End If
            '    End If
            ''--- fin validacion



            iolecmd_comand.Connection = Nothing
            iAdapt_comand.Dispose()
            iAdapt_comand = Nothing


            lint_counterTrys = lint_counterTrys + 1
            ' mientras el contador sea menor que el limite del archivo 

        Loop While lint_counterTrys < lint_tries

        Return idt_result

    End Function

    '''''''''''''
    ''''''''''''
    <WebMethod()> _
   Public Function Buscar_Datos_ContenedorVacio(ByVal strcontainerid As String) As DataTable
        Dim idt_result As DataTable = New DataTable ' Tabla con el query de resultados 
        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando
        Dim iolecmd_comand As OleDbCommand '' objeto comando que se ejecutara
        Dim ioleconx_conexion As OleDbConnection = New OleDbConnection() '' objeto de conexion que se usara para conectar 
        Dim istr_conx As String '' cadena de conexion
        'Dim strcontainerid As String = "IPXU3283286"
        istr_conx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        ioleconx_conexion.ConnectionString = istr_conx
        iolecmd_comand = ioleconx_conexion.CreateCommand()
        idt_result.TableName = "Buscar_Datos"

        Dim strSQL As String
        ''
        Dim llng_Timeout As Long = 0
        Dim lint_tries As Integer = 0
        Dim lint_counterTrys As Integer = 0
        Dim lint_HasError As Integer = 0
        Dim lint_IsTimeError As Integer = 0

        ' obtener el  valor de time out 
        llng_Timeout = of_getTimeoutSearch()
        ' obteneer la cantida de intentos 
        lint_tries = of_getSearchTries()

        'Dim strcontainerid As String


        'Sentencia SQL que recobra los datos para la pantalla de cambio de Ubicacion
        strSQL = "SELECT  tblclsContainerInventory.strContainerId, " & _
                         "tblclsContainerInventory.intContainerUniversalId, " & _
                         "tblclsContainerType.strContainerTypeIdentifier, " & _
                         "tblclsContainerSize.strContainerSizeIdentifier, " & _
                         "isnull(tblclsContainerInventory.decContainerInventoryWeight,0) as 'decContainerInventoryVGM', " & _
                         "tblclsContainer.decContainerTare, " & _
                         "tblclsContainerInventory.decContainerInventoryWeight, " & _
                         "tblclsContainerInventory.blnContainerIsFull, " & _
                         "(CASE ISNULL(tblclsContainerInventory.intContainerUniversalId, 0) " & _
                               "WHEN 0 THEN 'SIN ESTATUS' " & _
                               "ELSE tblclsContainerFiscalStatus.strContFisStatusIdentifier " & _
                          "END) AS 'strContFisStatusIdentifier' , " & _
                         "tblclsContainerAdmStatus.strContAdmStatusIdentifier, " & _
                         "tblclsContainerInventory.strContainerInvYardPositionId, " & _
                         "tblclsContainerInventory.strContainerInvComments, " & _
                         "DATEDIFF(dd, dtmContainerInvReceptionDate, GETDATE()) As intDaysInTerminal ," & _
                         "  VSS.strVesselName, VVY.vchVesselVoyageDescription " & _
                         " , VSS.strVesselName +'-'+ CONVERT(VARCHAR(2),DATEPART(dd,VVY.dteVesselVoyageArrivalDate)) +'/'+CONVERT(VARCHAR(2),DATEPART(mm,VVY.dteVesselVoyageArrivalDate)) + '/'+CONVERT(VARCHAR(4),DATEPART(yy,VVY.dteVesselVoyageArrivalDate))  AS strVesselAndDate " & _
                         " ,VSS.strVesselIdentifier + '-' +VVY.strVesselVoyageNumIdentifier as strVesselIdandVoyageId" & _
                         " ,SHIP.strShippingLineIdentifier " & _
                         " ,tblclsContainerInventory.strContainerInvComments " & _
                         " , CASE WHEN LEN( tblclsContainerInventory.strContainerInvFinalPortId) > 1 THEN tblclsContainerInventory.strContainerInvFinalPortId " & _
                         "   ELSE tblclsContainerInventory.strContainerInvDischargePortId" & _
                         "  END   AS strFinalPort " & _
                         " ,FMOV.strFiscalMovementIdentifier " & _
                         " ,( SELECT ISNULL(MAX( tblclsContainerSeal.strContainerSealNumber ),'') " & _
                         "     FROM tblclsContainerSeal " & _
                         "     WHERE tblclsContainerSeal.intContainerUniversalId = tblclsContainerInventory.intContainerUniversalId ) AS SEAL " & _
                         " ,( SELECT ISNULL(MAX(tblclsIMOCode.strIMOCodeIdentifier),'') " & _
                         "    FROM tblIMOCode_ContainerInventory " & _
                         "       INNER JOIN tblclsIMOCode ON tblclsIMOCode.intIMOCodeId = tblIMOCode_ContainerInventory.intIMOCodeId " & _
                         "    WHERE tblIMOCode_ContainerInventory.intContainerUniversalId = tblclsContainerInventory.intContainerUniversalId ) AS IMO " & _
                         " , CATE.strContainerCatIdentifier AS CATEGORY " & _
                         " FROM tblclsContainerInventory " & _
                         " LEFT JOIN tblclsContainerFiscalStatus " & _
                          " ON tblclsContainerInventory.intContFisStatusId = tblclsContainerFiscalStatus.intContFisStatusId " & _
                         " LEFT JOIN tblclsContainer " & _
                           " ON tblclsContainerInventory.strContainerId = tblclsContainer.strContainerId " & _
                         " LEFT JOIN tblclsContainerISOCode " & _
                          " ON tblclsContainer.intContISOCodeId = tblclsContainerISOCode.intContISOCodeId " & _
                         " LEFT JOIN tblclsContainerType " & _
                           " ON tblclsContainerISOCode.intContainerTypeId = tblclsContainerType.intContainerTypeId " & _
                         " LEFT JOIN tblclsContainerSize " & _
                           " ON tblclsContainerSize.intContainerSizeId  = tblclsContainerISOCode.intContainerSizeId " & _
                         " LEFT JOIN tblclsVesselVoyage VVY " & _
                           " ON  tblclsContainerInventory.intContainerInvVesselVoyageId = VVY.intVesselVoyageId  " & _
                         " LEFT JOIN tblclsVessel VSS " & _
                           "  ON VVY.intVesselId = VSS.intVesselId  " & _
                         " LEFT JOIN tblclsShippingLine SHIP " & _
                           " ON SHIP.intShippingLineId = tblclsContainerInventory.intContainerInvOperatorId  " & _
                         " LEFT JOIN tblclsFiscalMovement FMOV " & _
                           " ON FMOV.intFiscalMovementId  = tblclsContainerInventory.intFiscalMovementId " & _
                         " LEFT JOIN tblclsContainerAdmStatus " & _
                           " ON  tblclsContainerAdmStatus.intContAdmStatusId = tblclsContainerInventory.intContAdmStatusId " & _
                         " LEFT JOIN tblclsContainerCategory CATE " & _
                           " ON  tblclsContainerInventory.intContainerCategoryId  =  CATE.intContainerCategoryId  " & _
                   " WHERE ( tblclsContainerInventory.strContainerId = tblclsContainer.strContainerId ) and " & _
                         " ( tblclsContainerInventory.blnContainerInvActive = 1 ) and " & _
                         " (tblclsContainerInventory.strContainerId = '" & strcontainerid & "') "



        ''' CICLO
        ''
        Do
            idt_result = New DataTable
            iAdapt_comand = New OleDbDataAdapter()
            ioleconx_conexion = New OleDbConnection()
            istr_conx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
            ioleconx_conexion.ConnectionString = istr_conx
            iolecmd_comand = ioleconx_conexion.CreateCommand()
            idt_result.TableName = "Buscar_Datos"

            iolecmd_comand.CommandText = strSQL

            iAdapt_comand.SelectCommand = iolecmd_comand

            If llng_Timeout > 0 Then
                oleDBcom.CommandTimeout = llng_Timeout
            Else
                oleDBcom.CommandTimeout = 0
            End If

            Try
                iolecmd_comand.Connection.Open()
                iAdapt_comand.Fill(idt_result)
                lint_HasError = 0

                'si llego hasta aqui no hubo timeout
                lint_counterTrys = lint_tries

            Catch ex As Exception
                Dim strError As String
                strError = ObtenerError(ex.Message, 99999)
                idt_result = dt_RetrieveErrorTable(strError)

                If strError.ToLower.Contains("excedio") = True Or strError.ToLower.Contains("tiempo") = True Or strError.ToLower.Contains("time") = True Then
                    lint_IsTimeError = 1
                Else
                    lint_IsTimeError = 0
                    'hubo un error no era de timeout. ya forzar el ciclos
                    lint_counterTrys = lint_tries
                End If


            Finally
                iAdapt_comand.SelectCommand.Connection.Close()
                iolecmd_comand.Connection.Close()
                iolecmd_comand.Connection.Dispose()
                iAdapt_comand.SelectCommand.Connection.Dispose()
                ioleconx_conexion.Close()
                ioleconx_conexion.Dispose()

            End Try

            '' -- si el el resultado obtenido es de un renglon validar que el puerto , si es cadena vacia 
            'If idt_result.Rows.Count = 1 Then
            '    If IsDBNull(idt_result(0)("strFinalPort")) = True Then
            '        idt_result(0)("strFinalPort") = ""
            '        End If
            '    End If
            ''--- fin validacion
            iolecmd_comand.Connection = Nothing
            iAdapt_comand.Dispose()
            iAdapt_comand = Nothing

            lint_counterTrys = lint_counterTrys + 1

        Loop While lint_counterTrys < lint_tries

        Return idt_result

    End Function

    '''''''''''''''
    <WebMethod()> _
       Public Function Obtener_Clases() As DataTable
        Dim idt_result As DataTable = New DataTable ' Tabla con el query de resultados 
        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando
        Dim iolecmd_comand As OleDbCommand '' objeto comando que se ejecutara
        Dim ioleconx_conexion As OleDbConnection = New OleDbConnection() '' objeto de conexion que se usara para conectar 
        Dim istr_conx As String '' cadena de conexion
        'Dim strcontainerid As String = "IPXU3283286"
        istr_conx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        ioleconx_conexion.ConnectionString = istr_conx
        iolecmd_comand = ioleconx_conexion.CreateCommand()
        idt_result.TableName = "TrearDatos"
        Dim strSQL As String
        strSQL = "SELECT strContainerCatIdentifier, intContainerCategoryId , strContainerCatDescription FROM tblclsContainerCategory WHERE tblclsContainerCategory.blnContainerCatActive = 1 ORDER BY strContainerCatIdentifier"
        iolecmd_comand.CommandText = strSQL

        iAdapt_comand.SelectCommand = iolecmd_comand
        Try
            'ioleconx_conexion.Open()
            iAdapt_comand.SelectCommand.Connection.Open()
            iAdapt_comand.Fill(idt_result)
        Catch ex As Exception
            Dim strError As String
            strError = ObtenerError(ex.Message, 99999)
            If strError.Length = 0 Then
                strError = ex.Message
            End If
            idt_result = dt_RetrieveErrorTable("-" + strError + "-")
        Finally
            ioleconx_conexion.Close()
            iAdapt_comand.SelectCommand.Connection.Close()
        End Try
        Return idt_result
    End Function

    '''''''''''
    <WebMethod()> _
     Public Function Obtener_ListaClase(ByVal IDClase As Integer) As String
        ' Dim Categoria As String = "AR"
        Dim myConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        Dim myConnection As New OleDbConnection(myConnectionString)
        Dim mySelectQuery = "SELECT strContainerCatIdentifier FROM tblclsContainerCategory "
        Dim myCommand As New OleDbCommand(mySelectQuery, myConnection)

        Dim idt_result As DataTable = New DataTable ' Tabla con el query de resultados 
        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando
        Dim istr_Result As String



        '' inicializa en vacio la variable de resutlado 
        istr_Result = ""

        idt_result.TableName = "TrearDatos"
        'iolecmd_comand.CommandText = strSQL

        iAdapt_comand.SelectCommand = myCommand
        Try
            'ioleconx_conexion.Open()
            iAdapt_comand.SelectCommand.Connection.Open()
            iAdapt_comand.Fill(idt_result)
        Catch ex As Exception
            Dim strError As String
            strError = ObtenerError(ex.Message, 99999)
        Finally
            myConnection.Close()
            iAdapt_comand.SelectCommand.Connection.Close()
        End Try

        myConnection.Dispose()
        iAdapt_comand.SelectCommand.Connection.Dispose()
        iAdapt_comand.Dispose()

        myConnection = Nothing
        iAdapt_comand = Nothing

        If idt_result.Columns.Count = 1 And idt_result.Rows.Count = 1 Then

            istr_Result = idt_result(0)(0).ToString()

            If istr_Result.Length > 0 And istr_Result.Length < 17 Then '' generalment el nombre de la clase no es muy largo 
                Return istr_Result

            Else
                Return ""
            End If
        Else
            Return ""
        End If

        Return ""

    End Function

    ''''''''''''
    ''''''''''''

    ''''''''''''
    '''''''''''
    <WebMethod()> _
     Public Function Obtener_ListaClaseFilter(ByVal astr_clasePart As String) As DataTable
        ' Dim Categoria As String = "AR"
        Dim myConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        Dim myConnection As New OleDbConnection(myConnectionString)
        'Dim mySelectQuery = " SELECT tblclsContainerCategory.strContainerCatIdentifier FROM tblclsContainerCategory WHERE tblclsContainerCategory.strContainerCatIdentifier LIKE ?% "

        Dim mySelectQuery = " SELECT tblclsContainerCategory.intContainerCategoryId , tblclsContainerCategory.strContainerCatIdentifier FROM tblclsContainerCategory WHERE blnContainerCatActive = 1 and  tblclsContainerCategory.strContainerCatIdentifier LIKE '" + astr_clasePart + "%' "
        Dim myCommand As New OleDbCommand(mySelectQuery, myConnection)

        Dim idt_result As DataTable = New DataTable ' Tabla con el query de resultados 
        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando
        Dim oleDb_param As OleDbParameter = New OleDbParameter()
        Dim istr_Result As String



        '' inicializa en vacio la variable de resutlado 
        istr_Result = ""

        idt_result.TableName = "TrearDatos"
        'iolecmd_comand.CommandText = strSQL

        ''argumento
        'oleDb_param.ParameterName = "@strFilter"
        'oleDb_param.OleDbType = OleDbType.VarChar
        'oleDb_param.Value = astr_clasePart
        ''argumento
        'myCommand.Parameters.Add(oleDb_param)

        'definir comando 
        iAdapt_comand.SelectCommand = myCommand
        Try
            'ioleconx_conexion.Open()
            iAdapt_comand.SelectCommand.Connection.Open()
            iAdapt_comand.Fill(idt_result)
        Catch ex As Exception
            Dim strError As String
            strError = ObtenerError(ex.Message, 99999)
        Finally
            myConnection.Close()
            iAdapt_comand.SelectCommand.Connection.Close()
        End Try

        myConnection.Dispose()
        iAdapt_comand.SelectCommand.Connection.Dispose()
        iAdapt_comand.Dispose()

        myConnection = Nothing
        iAdapt_comand = Nothing

        Return idt_result

    End Function

    ''''''''''''
    ''''''''''''


    '''''''''''

    Public Function ObtenerError(ByVal cad As String, ByVal ex As Integer) As String

        If cad.Contains(ex.ToString) And cad.Contains("Sybase Provider]") Then
            Dim idx As Integer
            idx = cad.LastIndexOf("]")
            idx = idx + 1
            If idx > 0 And idx <= cad.Length Then
                Return cad.Substring(idx)
            Else
                Return ""
            End If
        Else
            If cad.Contains("Sybase Provider]") Then
                Dim idx As Integer
                idx = cad.LastIndexOf("]")
                idx = idx + 1
                If idx > 0 And idx <= cad.Length Then
                    Return cad.Substring(idx)
                Else
                    Return ""
                End If

                '' sino retornar cadena 
            Else
                Return cad
            End If
        End If
        Return ""
    End Function



    <WebMethod()> _
   Public Function CambiarPosicionPatio(ByVal prmint_ContainerUniv As Integer, ByVal prmstr_PosicionPatioFin As String, ByVal prmint_AtachID As Integer, ByVal prmstr_PosicionOrigen As String, ByVal prmstr_Username As String) As String

        '-----------------------------
        Dim oleDBconnx As OleDbConnection
        Dim oleDBcom As OleDbCommand
        Dim ls_position As String
        Dim strconx As String
        Dim lint_universal As Integer

        Dim olprm_attachid As OleDbParameter
        Dim dfind_dataadapter As OleDbDataAdapter
        Dim dfind_table As DataTable = New DataTable()

        oleDBconnx = New OleDbConnection()

        oleDBconnx.ConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString

        oleDBcom = oleDBconnx.CreateCommand

        '----------------------------------

        'variables locales

        Dim ls_sqlcom As String
        Dim ls_strContainerId As String

        Dim li_poslength As Integer
        Dim ls_Block As String
        Dim ls_Row As String
        Dim ls_Bay As String
        Dim ls_Stow As String
        Dim li_bSuccess As Integer = 0
        Dim ls_comments As String

        'declaracion de variables de base de datos

        Dim lodb_UniversalID As OleDbParameter = New OleDbParameter()
        Dim lodb_SPosition As OleDbParameter = New OleDbParameter()
        Dim lodb_SBlock As OleDbParameter = New OleDbParameter()
        Dim lodb_SRow As OleDbParameter = New OleDbParameter()
        Dim lodb_SBay As OleDbParameter = New OleDbParameter()
        Dim lodb_SStow As OleDbParameter = New OleDbParameter()
        Dim lodb_ErrorCode As OleDbParameter = New OleDbParameter()
        Dim lodb_UserName As OleDbParameter = New OleDbParameter()
        Dim lodb_iAttachId As OleDbParameter = New OleDbParameter()
        Dim lodb_sComments As OleDbParameter = New OleDbParameter()
        Dim lodb_sUser As OleDbParameter = New OleDbParameter()
        Dim lodb_dataReader As OleDbDataReader


        ' variables locales, tendran los valores de los controles
        ls_position = prmstr_PosicionPatioFin.Trim()

        'obtencion de la bahia ,bloque  y estiba 

        li_poslength = prmstr_PosicionPatioFin.Length
        If li_poslength < 6 Then
            ls_Stow = ""
        Else
            ls_Stow = ls_position.Substring(li_poslength - 1)
        End If
        If li_poslength < 5 Then
            ls_Row = ""
        Else
            ls_Row = ls_position.Substring(li_poslength - 2, 1)
        End If
        If li_poslength < 4 Then
            ls_Bay = ""
        Else
            ls_Bay = ls_position.Substring(li_poslength - 4, 2)
        End If
        If li_poslength < 3 Then
            ls_Block = ""
        Else
            ls_Block = ls_position.Substring(0, li_poslength - 4)
        End If

        ls_comments = ""

        ' si el contenedor no tiene id de atado se hara lo normal
        If prmint_AtachID = 0 Then

            If prmint_ContainerUniv > 0 Then

                'asignacion de los argumentos al procedimiento

                lodb_UniversalID.OleDbType = OleDbType.Integer
                lodb_UniversalID.ParameterName = "@intUniversalId"
                lodb_UniversalID.Value = prmint_ContainerUniv

                lodb_SPosition.OleDbType = OleDbType.VarChar
                lodb_SPosition.ParameterName = "@strYardPosition"
                lodb_SPosition.Size = 20
                lodb_SPosition.Value = ls_position

                lodb_SBlock.OleDbType = OleDbType.VarChar
                lodb_SBlock.ParameterName = "@strBlockIdentifier"
                lodb_SBlock.Size = 10
                lodb_SBlock.Value = ls_Block

                lodb_SRow.OleDbType = OleDbType.VarChar
                lodb_SRow.ParameterName = "@strInvPosRow"
                lodb_SRow.Size = 10
                lodb_SRow.Value = ls_Row

                lodb_SBay.OleDbType = OleDbType.VarChar
                lodb_SBay.ParameterName = "@strInvPosBay"
                lodb_SBay.Size = 10
                lodb_SBay.Value = ls_Bay

                lodb_SStow.OleDbType = OleDbType.VarChar
                lodb_SStow.ParameterName = "@strInvPosStow"
                lodb_SStow.Size = 10
                lodb_SStow.Value = ls_Stow

                lodb_ErrorCode.OleDbType = OleDbType.Integer
                lodb_ErrorCode.Size = 12
                lodb_ErrorCode.ParameterName = "@intErrorCode"
                lodb_ErrorCode.Direction = ParameterDirection.Output

                lodb_sComments.OleDbType = OleDbType.VarChar
                lodb_sComments.Size = 27
                lodb_sComments.ParameterName = "@Comments"

                ls_sqlcom = "spUpdatePositionInventory"

                '' se asignaa el comando al procedimiento
                oleDBcom.Parameters.Add(lodb_UniversalID)
                oleDBcom.Parameters.Add(lodb_SPosition)
                oleDBcom.Parameters.Add(lodb_SBlock)
                oleDBcom.Parameters.Add(lodb_SRow)
                oleDBcom.Parameters.Add(lodb_SBay)
                oleDBcom.Parameters.Add(lodb_SStow)
                oleDBcom.Parameters.Add(lodb_ErrorCode)

                oleDBcom.CommandType = CommandType.StoredProcedure
                oleDBcom.CommandText = ls_sqlcom
                oleDBcom.CommandTimeout = 0


                '' se ejecuta el comando
                ''
                Try
                    oleDBconnx.Open()
                    oleDBcom.ExecuteNonQuery()
                    li_bSuccess = 1

                Catch ex As Exception
                    li_bSuccess = 0
                    Dim stresult As String = ObtenerError(ex.Message, 99999)
                    If stresult.Length > 0 Then
                        If stresult.IndexOf("imeout exceeded") > 0 Then
                            stresult = "Error en base de datos"
                        End If

                        Return stresult
                    Else
                        If ex.Message.IndexOf("imeout exceeded") > 0 Then
                            Return "Error en base de datos"
                        End If

                        Return ex.Message
                    End If
                Finally
                    oleDBconnx.Close()
                    oleDBcom.Connection.Close()
                End Try
                'Return ""

                '''''''''--------------------------  Actualizar posicion de el historico ----------------------------------------------

                'limpieza de parametros 
                oleDBcom.Parameters.Clear()

                'generacion de los comentarios 
                ls_comments = " De " + prmstr_PosicionOrigen + "a:" + prmstr_PosicionPatioFin



                '' se especifican los parametros que se van a usar

                lodb_UniversalID.OleDbType = OleDbType.Integer
                lodb_UniversalID.ParameterName = "@UniversalId"
                lodb_UniversalID.Value = prmint_ContainerUniv

                lodb_SPosition.OleDbType = OleDbType.VarChar
                lodb_SPosition.ParameterName = "@YardPosId"
                lodb_SPosition.Size = 20
                lodb_SPosition.Value = ls_position

                lodb_SBlock.OleDbType = OleDbType.VarChar
                lodb_SBlock.ParameterName = "@Block"
                lodb_SBlock.Size = 10
                lodb_SBlock.Value = ls_Block

                lodb_SRow.OleDbType = OleDbType.VarChar
                lodb_SRow.ParameterName = "@Row"
                lodb_SRow.Size = 10
                lodb_SRow.Value = ls_Row

                lodb_SBay.OleDbType = OleDbType.VarChar
                lodb_SBay.ParameterName = "@Bay"
                lodb_SBay.Size = 10
                lodb_SBay.Value = ls_Bay

                lodb_SStow.OleDbType = OleDbType.VarChar
                lodb_SStow.ParameterName = "@Stow"
                lodb_SStow.Size = 10
                lodb_SStow.Value = ls_Stow

                lodb_sComments.OleDbType = OleDbType.VarChar
                lodb_sComments.ParameterName = "@Comments"
                lodb_sComments.Size = 100
                lodb_sComments.Value = ls_comments

                lodb_sUser.OleDbType = OleDbType.VarChar
                lodb_sUser.Size = 25
                lodb_sUser.ParameterName = "@User"
                lodb_sUser.Value = prmstr_Username



                ls_sqlcom = "spUpdateHistoryContPosition"

                '' se asignaa el comando al procedimiento
                oleDBcom.Parameters.Add(lodb_UniversalID)
                oleDBcom.Parameters.Add(lodb_SPosition)
                oleDBcom.Parameters.Add(lodb_SBlock)
                oleDBcom.Parameters.Add(lodb_SRow)
                oleDBcom.Parameters.Add(lodb_SBay)
                oleDBcom.Parameters.Add(lodb_SStow)
                oleDBcom.Parameters.Add(lodb_sComments)
                oleDBcom.Parameters.Add(lodb_sUser)

                oleDBcom.CommandType = CommandType.StoredProcedure
                oleDBcom.CommandText = ls_sqlcom
                oleDBcom.CommandTimeout = 0


                'ejecucion del procedimiento


                Try
                    oleDBconnx.Open()
                    oleDBcom.ExecuteNonQuery()
                    li_bSuccess = 1

                Catch ex As Exception

                    li_bSuccess = 0

                    Dim stresult As String = ObtenerError(ex.Message, 99999)

                    If stresult.Length > 0 Then
                        Return stresult
                    End If

                Finally

                    oleDBconnx.Close()

                End Try

                Return ""

            Else

                Return "Error de conexion"

            End If

            ''''-----------------------este para si tiene atado el id ---------------------------------------------------

        Else

            'oleDBcom.Parameters.Clear()
            'oleDBcomAttach.Parameters.Clear()

            ls_sqlcom = " SELECT intContainerUniversalId FROM tblclsContainerInvAttachedItem WHERE intContInvAttachId = ?"

            '' se ejecuta el comando para obtener los ids de los atados 

            lodb_iAttachId.OleDbType = OleDbType.Integer
            lodb_iAttachId.ParameterName = "@intAttachedId"
            lodb_iAttachId.Direction = ParameterDirection.Input
            lodb_iAttachId.Value = prmint_AtachID

            oleDBcom.Parameters.Clear()
            oleDBcom.Parameters.Add(lodb_iAttachId)

            oleDBcom.CommandTimeout = 0

            oleDBcom.CommandText = ls_sqlcom

            dfind_dataadapter = New OleDbDataAdapter(oleDBcom)


            '' ejecucion de el comando para obtener el atado


            Try
                oleDBconnx.Open()
                dfind_dataadapter.Fill(dfind_table)

            Catch ex As Exception

                Return " el contenedor no tiene atados"

            Finally

                oleDBconnx.Close()

            End Try



            For Each renglon As DataRow In dfind_table.Rows

                ''''' obtener el universal de el inventario

                lint_universal = CType(renglon.Item("intContainerUniversalId"), System.Int32)

                '''' si el comando tiene un universal mayor a 0 
                ''''' 
                If lint_universal > 0 Then

                    oleDBcom.Parameters.Clear()
                    ''''' ejecutar el comando de posicion de el inventario

                    lodb_UniversalID.Value = lint_universal
                    lodb_UniversalID.OleDbType = OleDbType.Integer

                    lodb_SPosition.Value = ls_position
                    lodb_SPosition.OleDbType = OleDbType.VarChar
                    lodb_SPosition.Size = 10

                    lodb_SBlock.Value = ls_Block
                    lodb_SBlock.OleDbType = OleDbType.VarChar
                    lodb_SBlock.Size = 5


                    lodb_SRow.OleDbType = OleDbType.VarChar
                    lodb_SRow.Value = ls_Row
                    lodb_SRow.Size = 5

                    lodb_SBay.OleDbType = OleDbType.VarChar
                    lodb_SBay.Value = ls_Bay
                    lodb_SBay.Size = 5

                    lodb_SStow.OleDbType = OleDbType.VarChar
                    lodb_SStow.Value = ls_Stow
                    lodb_SStow.Size = 5

                    lodb_sComments.OleDbType = OleDbType.VarChar
                    lodb_sComments.Value = ls_comments
                    lodb_sComments.Size = 100

                    lodb_sUser.Value = prmstr_Username
                    lodb_sUser.OleDbType = OleDbType.VarChar
                    lodb_sUser.Size = 25

                    lodb_ErrorCode.OleDbType = OleDbType.Integer
                    lodb_ErrorCode.Size = 12
                    lodb_ErrorCode.ParameterName = "@intErrorCode"
                    lodb_ErrorCode.Direction = ParameterDirection.Output


                    ls_sqlcom = "spUpdatePositionInventory"

                    '' se asignaa el comando al procedimiento

                    oleDBcom.Parameters.Add(lodb_UniversalID)
                    oleDBcom.Parameters.Add(lodb_SPosition)
                    oleDBcom.Parameters.Add(lodb_SBlock)
                    oleDBcom.Parameters.Add(lodb_SRow)
                    oleDBcom.Parameters.Add(lodb_SBay)
                    oleDBcom.Parameters.Add(lodb_SStow)
                    oleDBcom.Parameters.Add(lodb_ErrorCode)

                    oleDBcom.CommandType = CommandType.StoredProcedure
                    oleDBcom.CommandText = ls_sqlcom
                    oleDBcom.CommandTimeout = 0


                    'ejecucion del procedimiento


                    Try
                        oleDBconnx.Open()
                        oleDBcom.ExecuteNonQuery()
                        li_bSuccess = 1

                    Catch ex As Exception

                        li_bSuccess = 0

                        Dim stresult As String = ObtenerError(ex.Message, 99999)

                        If stresult.Length > 0 Then
                            Return stresult
                        End If

                    Finally

                        oleDBconnx.Close()

                    End Try



                    ''''' ejecutar el comando de historico

                    ''''''''''-----------------------------  -----------------------------------------------
                    'limpieza de parametros 
                    oleDBcom.Parameters.Clear()

                    'generacion de los comentarios 
                    ls_comments = " De " + prmstr_PosicionOrigen + " a: " + prmstr_PosicionPatioFin



                    '' se especifican los parametros que se van a usar

                    lodb_UniversalID.OleDbType = OleDbType.Integer
                    lodb_UniversalID.ParameterName = "@UniversalId"
                    lodb_UniversalID.Value = lint_universal

                    lodb_SPosition.OleDbType = OleDbType.VarChar
                    lodb_SPosition.ParameterName = "@YardPosId"
                    lodb_SPosition.Size = 20
                    lodb_SPosition.Value = ls_position

                    lodb_SBlock.OleDbType = OleDbType.VarChar
                    lodb_SBlock.ParameterName = "@Block"
                    lodb_SBlock.Size = 10
                    lodb_SBlock.Value = ls_Block

                    lodb_SRow.OleDbType = OleDbType.VarChar
                    lodb_SRow.ParameterName = "@Row"
                    lodb_SRow.Size = 10
                    lodb_SRow.Value = ls_Row

                    lodb_SBay.OleDbType = OleDbType.VarChar
                    lodb_SBay.ParameterName = "@Bay"
                    lodb_SBay.Size = 10
                    lodb_SBay.Value = ls_Bay

                    lodb_SStow.OleDbType = OleDbType.VarChar
                    lodb_SStow.ParameterName = "@Stow"
                    lodb_SStow.Size = 10
                    lodb_SStow.Value = ls_Stow

                    lodb_sComments.OleDbType = OleDbType.VarChar
                    lodb_sComments.ParameterName = "@Comments"
                    lodb_sComments.Size = 100
                    lodb_sComments.Value = ls_comments

                    lodb_sUser.OleDbType = OleDbType.VarChar
                    lodb_sUser.Size = 25
                    lodb_sUser.ParameterName = "@User"
                    lodb_sUser.Value = prmstr_Username



                    ls_sqlcom = "spUpdateHistoryContPosition"

                    '' se asignaa el comando al procedimiento
                    oleDBcom.Parameters.Add(lodb_UniversalID)
                    oleDBcom.Parameters.Add(lodb_SPosition)
                    oleDBcom.Parameters.Add(lodb_SBlock)
                    oleDBcom.Parameters.Add(lodb_SRow)
                    oleDBcom.Parameters.Add(lodb_SBay)
                    oleDBcom.Parameters.Add(lodb_SStow)
                    oleDBcom.Parameters.Add(lodb_sComments)
                    oleDBcom.Parameters.Add(lodb_sUser)

                    oleDBcom.CommandType = CommandType.StoredProcedure
                    oleDBcom.CommandText = ls_sqlcom
                    oleDBcom.CommandTimeout = 0


                    'ejecucion del procedimiento


                    Try
                        oleDBconnx.Open()
                        oleDBcom.ExecuteNonQuery()
                        li_bSuccess = 1

                    Catch ex As Exception

                        li_bSuccess = 0

                        Dim stresult As String = ObtenerError(ex.Message, 99999)

                        If stresult.Length > 0 Then
                            Return stresult
                        End If

                    Finally

                        oleDBconnx.Close()

                    End Try


                Else

                    Return "Error de conexion"

                End If

            Next


        End If


        Return ""


    End Function


    <WebMethod()> _
   Public Function ConfirmarUbicacionPatio(ByVal prmint_ContainerUniv As Integer, ByVal prmstr_PosicionPatioFin As String, ByVal prmstr_Username As String) As String

        '-----------------------------
        Dim oleDBconnx As OleDbConnection
        Dim oleDBcom As OleDbCommand
        Dim ls_position As String
        Dim strconx As String
        Dim lint_universal As Integer

        Dim olprm_attachid As OleDbParameter
        Dim dfind_dataadapter As OleDbDataAdapter
        Dim dfind_table As DataTable = New DataTable()

        oleDBconnx = New OleDbConnection()

        oleDBconnx.ConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString

        oleDBcom = oleDBconnx.CreateCommand

        '----------------------------------

        'variables locales

        Dim ls_sqlcom As String
        Dim ls_strContainerId As String

        Dim li_poslength As Integer
        Dim ls_Block As String
        Dim ls_Row As String
        Dim ls_Bay As String
        Dim ls_Stow As String
        Dim li_bSuccess As Integer = 0
        Dim ls_comments As String

        'declaracion de variables de base de datos

        Dim lodb_UniversalID As OleDbParameter = New OleDbParameter()
        Dim lodb_SPosition As OleDbParameter = New OleDbParameter()
        Dim lodb_SBlock As OleDbParameter = New OleDbParameter()
        Dim lodb_SRow As OleDbParameter = New OleDbParameter()
        Dim lodb_SBay As OleDbParameter = New OleDbParameter()
        Dim lodb_SStow As OleDbParameter = New OleDbParameter()
        Dim lodb_ErrorCode As OleDbParameter = New OleDbParameter()
        Dim lodb_UserName As OleDbParameter = New OleDbParameter()
        Dim lodb_iAttachId As OleDbParameter = New OleDbParameter()
        Dim lodb_sUser As OleDbParameter = New OleDbParameter()
        Dim lodb_dataReader As OleDbDataReader


        ' variables locales, tendran los valores de los controles
        ls_position = prmstr_PosicionPatioFin.Trim()

        'obtencion de la bahia ,bloque  y estiba 

        li_poslength = prmstr_PosicionPatioFin.Length
        If li_poslength < 6 Then
            ls_Stow = ""
        Else
            ls_Stow = ls_position.Substring(li_poslength - 1)
        End If
        If li_poslength < 5 Then
            ls_Row = ""
        Else
            ls_Row = ls_position.Substring(li_poslength - 2, 1)
        End If
        If li_poslength < 4 Then
            ls_Bay = ""
        Else
            ls_Bay = ls_position.Substring(li_poslength - 4, 2)
        End If
        If li_poslength < 3 Then
            ls_Block = ""
        Else
            ls_Block = ls_position.Substring(0, li_poslength - 4)
        End If

        ls_comments = ""


        If prmint_ContainerUniv > 0 Then

            'asignacion de los argumentos al procedimiento

            lodb_UniversalID.OleDbType = OleDbType.Integer
            lodb_UniversalID.ParameterName = "@intUniversalId"
            lodb_UniversalID.Value = prmint_ContainerUniv

            lodb_SPosition.OleDbType = OleDbType.VarChar
            lodb_SPosition.ParameterName = "@strYardPosition"
            lodb_SPosition.Size = 20
            lodb_SPosition.Value = ls_position

            lodb_SBlock.OleDbType = OleDbType.VarChar
            lodb_SBlock.ParameterName = "@strBlockIdentifier"
            lodb_SBlock.Size = 10
            lodb_SBlock.Value = ls_Block

            lodb_SRow.OleDbType = OleDbType.VarChar
            lodb_SRow.ParameterName = "@strInvPosRow"
            lodb_SRow.Size = 10
            lodb_SRow.Value = ls_Row

            lodb_SBay.OleDbType = OleDbType.VarChar
            lodb_SBay.ParameterName = "@strInvPosBay"
            lodb_SBay.Size = 10
            lodb_SBay.Value = ls_Bay

            lodb_SStow.OleDbType = OleDbType.VarChar
            lodb_SStow.ParameterName = "@strInvPosStow"
            lodb_SStow.Size = 10
            lodb_SStow.Value = ls_Stow

            lodb_sUser.OleDbType = OleDbType.VarChar
            lodb_sUser.Size = 17
            lodb_sUser.ParameterName = "@astrUserName"


            lodb_ErrorCode.OleDbType = OleDbType.Integer
            lodb_ErrorCode.Size = 12
            lodb_ErrorCode.ParameterName = "@intErrorCode"
            lodb_ErrorCode.Direction = ParameterDirection.Output

            ls_sqlcom = "spConfirmContainerYardPosHH"

            '' se asignaa el comando al procedimiento
            oleDBcom.Parameters.Add(lodb_UniversalID)
            oleDBcom.Parameters.Add(lodb_SPosition)
            oleDBcom.Parameters.Add(lodb_SBlock)
            oleDBcom.Parameters.Add(lodb_SRow)
            oleDBcom.Parameters.Add(lodb_SBay)
            oleDBcom.Parameters.Add(lodb_SStow)
            oleDBcom.Parameters.Add(lodb_sUser)
            oleDBcom.Parameters.Add(lodb_ErrorCode)

            oleDBcom.CommandType = CommandType.StoredProcedure
            oleDBcom.CommandText = ls_sqlcom
            oleDBcom.CommandTimeout = 0


            '' se ejecuta el comando
            ''
            Try
                oleDBconnx.Open()
                oleDBcom.ExecuteNonQuery()
                li_bSuccess = 1

            Catch ex As Exception
                li_bSuccess = 0
                Dim stresult As String = ObtenerError(ex.Message, 99999)
                If stresult.Length > 0 Then
                    If stresult.IndexOf("imeout exceeded") > 0 Then
                        stresult = "Error en base de datos"
                    End If

                    Return stresult
                Else
                    If ex.Message.IndexOf("imeout exceeded") > 0 Then
                        Return "Error en base de datos"
                    End If

                    Return ex.Message
                End If
            Finally
                oleDBconnx.Close()
                oleDBcom.Connection.Close()
            End Try
            'Return ""

        End If 'If prmint_ContainerUniv > 0 Then


        Return ""


    End Function


    '''''''''''''''
    <WebMethod()> _
    Public Function BuscarItemsVisitaID_Placa(ByVal prmint_VisitId As Integer, ByVal prmstr_VisitPlate As String) As DataTable

        '-----------------------------
        Dim oleDBconnx As OleDbConnection
        Dim oleDBconnx_gc As OleDbConnection

        Dim oleDBcom As OleDbCommand
        Dim oleDBcom_gc As OleDbCommand

        Dim lint_Visitnumber As Long
        Dim lstr_visitplate As String
        Dim strconx As String

        Dim lint_universal As Integer


        Dim dfind_dataadapter As OleDbDataAdapter
        Dim dfind_dataadapter_gc As OleDbDataAdapter

        Dim dfind_table As DataTable = New DataTable()
        Dim dfind_table_gc As DataTable = New DataTable()


        oleDBconnx = New OleDbConnection()
        oleDBconnx.ConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString

        oleDBcom = oleDBconnx.CreateCommand

        '----------------------------------

        'variables locales
        Dim ls_sqlcom As String
        Dim ls_strContainerId As String

        Dim li_poslength As Integer
        Dim ls_Block As String
        Dim ls_Row As String
        Dim ls_Bay As String
        Dim ls_Stow As String
        Dim li_bSuccess As Integer = 0
        Dim ls_comments As String

        'declaracion de variables de base de datos
        Dim lodb_VisitNumber As OleDbParameter = New OleDbParameter()
        Dim lodb_VisitNumber_gc As OleDbParameter = New OleDbParameter()

        Dim lodb_VisitPlate As OleDbParameter = New OleDbParameter()
        Dim lodb_VisitPlate_gc As OleDbParameter = New OleDbParameter()

        Dim lodb_dataReader As OleDbDataReader
        Dim lodb_dataReader_gc As OleDbDataReader
        Dim DataResult As DataTable
        Dim DataResult_gc = New Data.DataTable() 'DataSet = New DataSet(
        Dim adapter As OleDbDataAdapter = New OleDbDataAdapter(oleDBcom)


        'varaibles y valores locales


        ' variables locales, tendran los valores de los controles
        lstr_visitplate = prmstr_VisitPlate.Trim()

        lstr_visitplate = lstr_visitplate.ToUpper()
        'obtencion de la bahia ,bloque  y estiba 

        ''*************************
        ''ciclo1

        oleDBconnx = New OleDbConnection()
        oleDBconnx_gc = New OleDbConnection()

        oleDBcom = New OleDbCommand
        oleDBcom_gc = New OleDbCommand

        lint_Visitnumber = 0
        lstr_visitplate = ""
        strconx = ""

        lint_universal = 0

        dfind_dataadapter = New OleDbDataAdapter()
        dfind_dataadapter_gc = New OleDbDataAdapter()

        dfind_table = New DataTable()
        dfind_table_gc = New DataTable()

        DataResult = New DataTable()

        oleDBconnx = New OleDbConnection()
        oleDBconnx.ConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString

        oleDBcom = oleDBconnx.CreateCommand

        Dim llng_Timeout As Long = 0
        Dim lint_tries As Integer = 0
        Dim lint_counterTrys As Integer = 0
        Dim lint_HasError As Integer = 0
        Dim lint_IsTimeError As Integer = 0
        Dim strError As String

        ' obtener el  valor de time out 
        llng_Timeout = of_getTimeoutSearch()
        ' obteneer la cantida de intentos 
        lint_tries = of_getSearchTries()


        '----------------------------------
        'ciclo1
        Do
            'variables locales
            ls_sqlcom = ""
            strError = ""

            lstr_visitplate = prmstr_VisitPlate.Trim()
            lstr_visitplate = lstr_visitplate.ToUpper()

            If llng_Timeout > 0 Then
                oleDBcom.CommandTimeout = llng_Timeout
            Else
                oleDBcom.CommandTimeout = 0
            End If
            'oleDBcom.CommandTimeout = 0

            lodb_VisitNumber = New OleDbParameter()
            lodb_VisitNumber_gc = New OleDbParameter()

            lodb_VisitPlate = New OleDbParameter()
            lodb_VisitPlate_gc = New OleDbParameter()

            'lodb_dataReader = New OleDbDataReader()
            'lodb_dataReader_gc As OleDbDataReader

            dfind_dataadapter = New OleDbDataAdapter(oleDBcom)

            '''********************************************

            lint_Visitnumber = prmint_VisitId

            lodb_VisitNumber.OleDbType = OleDbType.Integer
            lodb_VisitNumber.ParameterName = "@aintVisitId"
            lodb_VisitNumber.Value = lint_Visitnumber

            lodb_VisitPlate.OleDbType = OleDbType.VarChar
            lodb_VisitPlate.ParameterName = "@astrVisitPlate"
            lodb_VisitPlate.Value = 20
            lodb_VisitPlate.Value = lstr_visitplate

            ls_sqlcom = "spGetVisitItemsByIDorPlate"

            oleDBcom.Parameters.Add(lodb_VisitNumber)
            oleDBcom.Parameters.Add(lodb_VisitPlate)

            oleDBcom.CommandType = CommandType.StoredProcedure
            oleDBcom.CommandText = ls_sqlcom
            'oleDBcom.CommandTimeout = 0

            DataResult = New Data.DataTable() 'DataSet = New DataSet()
            DataResult.TableName = "TrearDatos"

            adapter = New OleDbDataAdapter(oleDBcom)

            Try
                oleDBconnx.Open()
                'oleDBcom.ExecuteNonQuery()
                adapter.Fill(DataResult)

                lint_HasError = 0
                'si llego hasta aqui no hubo timeout
                lint_counterTrys = lint_tries

            Catch ex As Exception
                'Dim strError As String
                strError = ObtenerError(ex.Message, 99999)


                If strError.ToLower.Contains("excedio") = True Or strError.ToLower.Contains("tiempo") = True Or strError.ToLower.Contains("time") = True Then
                    lint_IsTimeError = 1
                Else
                    lint_IsTimeError = 0
                    'hubo un error no era de timeout. ya forzar el ciclos
                    lint_counterTrys = lint_tries
                End If


                'Return dt_RetrieveErrorTable(strError)
            Finally
                If oleDBconnx IsNot Nothing Then
                    oleDBconnx.Close()
                    oleDBconnx.Dispose()
                End If

                oleDBconnx = Nothing

            End Try

            lint_counterTrys = lint_counterTrys + 1

            'fin ciclo1
        Loop While lint_counterTrys < lint_tries

        If strError.Length > 0 Then
            Return dt_RetrieveErrorTable(strError)
        End If
        '****************************
        '*************************
        '*******************************

        Dim lint_counter_cont As Integer = 0
        Dim lint_counter_gc As Integer = 0
        Dim lstr_servicecont As String = ""


        '' revisar si hay registro , si hay servicio 
        '' si no tiene strService , hacer que lint_counter_cont =0 
        Try
            lint_counter_cont = DataResult.Rows.Count
            If lint_counter_cont > 0 Then
                lstr_servicecont = DataResult(0)("strServiceIdentifier").ToString()
            End If
        Catch ex As Exception

        End Try


        ' obtener el  valor de time out 
        llng_Timeout = of_getTimeoutSearch()
        ' obteneer la cantida de intentos 
        lint_tries = of_getSearchTries()

        ''''ciclo2
        Do

            lstr_visitplate = prmstr_VisitPlate.Trim()
            lstr_visitplate = lstr_visitplate.ToUpper()

            strError = ""

            If llng_Timeout > 0 Then
                oleDBcom.CommandTimeout = llng_Timeout
            Else
                oleDBcom.CommandTimeout = 0
            End If

            DataResult_gc = New Data.DataTable() 'DataSet = New DataSet(
            '' agregar la busqueda de carga general, si el no hay registros encontrados 
            If DataResult.Rows.Count = 0 Or lstr_servicecont.Length < 2 Then

                oleDBconnx_gc = New OleDbConnection()
                oleDBconnx_gc.ConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString

                oleDBcom_gc = New OleDbCommand()
                oleDBcom_gc = oleDBconnx_gc.CreateCommand
                oleDBcom_gc.Parameters.Clear()


                lodb_VisitNumber_gc.OleDbType = OleDbType.Integer
                lodb_VisitNumber_gc.ParameterName = "@aintVisitId"
                lodb_VisitNumber_gc.Value = lint_Visitnumber

                lodb_VisitPlate_gc.OleDbType = OleDbType.VarChar
                lodb_VisitPlate_gc.ParameterName = "@astrVisitPlate"
                lodb_VisitPlate_gc.Value = 20
                lodb_VisitPlate_gc.Value = lstr_visitplate


                oleDBcom_gc.Parameters.Add(lodb_VisitNumber_gc)
                oleDBcom_gc.Parameters.Add(lodb_VisitPlate_gc)

                ls_sqlcom = "spGetVisitGCByIDorPlate"

                oleDBcom_gc.CommandType = CommandType.StoredProcedure
                oleDBcom_gc.CommandText = ls_sqlcom
                'oleDBcom.CommandTimeout = 7

                DataResult_gc.TableName = "TrearDatos"

                dfind_dataadapter_gc = New OleDbDataAdapter(oleDBcom_gc)

                Try
                    oleDBconnx_gc.Open()
                    'oleDBcom.ExecuteNonQuery()
                    dfind_dataadapter_gc.Fill(DataResult_gc)

                    'revisar si gc tiene resultados 
                    lint_counter_gc = DataResult_gc.Rows.Count()

                    lint_HasError = 0
                    'se termino la consulta sin llegar a timeout
                    lint_counterTrys = lint_tries

                Catch ex As Exception

                    'Dim strError As String
                    strError = ObtenerError(ex.Message, 99999)

                    If strError.ToLower.Contains("excedio") = True Or strError.ToLower.Contains("tiempo") = True Or strError.ToLower.Contains("time") = True Then
                        lint_IsTimeError = 1
                    Else
                        lint_IsTimeError = 0
                        'hubo un error no era de timeout. ya forzar el ciclos
                        lint_counterTrys = lint_tries
                    End If

                    ' Return dt_RetrieveErrorTable(strError)

                Finally
                    oleDBconnx_gc.Close()
                    oleDBconnx_gc.Dispose()
                    oleDBconnx_gc = Nothing

                End Try


            End If ' If DataResult.Rows.Count = 0 Then

            '*
            '*
            '*
            lint_counterTrys = lint_counterTrys + 1
            ' mientras el contador sea menor que el limite del archivo 

        Loop While lint_counterTrys < lint_tries
        '''' fin ciclo2 

        'si hubo error ,retornar mensaje 
        If strError.Length > 0 Then
            Return dt_RetrieveErrorTable(strError)
        End If


        'ver quer retornar si lint_conter_conts o lint_counter_gc

        ' si no hay nada en constr, pero si hay gc 
        If lint_counter_cont = 0 And lint_counter_gc > 0 Then
            Return DataResult_gc
        End If 'If lint_counter_cont = 0 And lint_counter_gc > 0 Then

        ' si hay renglones de carga general y comlumnas de carga genreal traerla 
        If DataResult_gc.Rows.Count > 0 And DataResult_gc.Columns.Count > 2 Then
            Return DataResult_gc
        End If 'If lint_counter_cont = 0 And lint_counter_gc > 0 Then



        ' si hay registros de contenedores 
        If lint_counter_cont > 0 Then

            'nombre del servicio
            If lstr_servicecont.Length > 2 Or DataResult.Columns.Count > 2 Then
                'retornar dataresult
                Return DataResult
            Else 'si no hay servicio
                'si hay carga general , retornar carga general
                If DataResult_gc.Rows.Count > 0 Then
                    Return DataResult_gc
                End If

            End If '  If lstr_servicecont.Length > 0 Then

        End If 'If lint_counter_cont > 0 Then


        Return DataResult

    End Function



    ' <WebMethod()> _
    'Public Function spInOutVisit(ByVal Visita As Integer, ByVal UserName As String, ByVal cadena As String) As Integer


    '     Dim param As New OleDbParameter
    '     Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando
    '     Dim ldt_TableResult As DataTable = New DataTable("Result")
    '     param.ParameterName = ParameterDirection.ReturnValue

    '     Dim x As Integer = 666
    '     Dim oleDBconnx As OleDbConnection
    '     Dim oleDBcom As OleDbCommand
    '     oleDBcom = New OleDbCommand()
    '     oleDBconnx = New OleDbConnection()
    '     Dim strconx As String
    '     strconx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
    '     oleDBconnx.ConnectionString = strconx
    '     oleDBcom = oleDBconnx.CreateCommand
    '     '----------------------------------
    '     Dim oleDb_param As OleDbParameter = New OleDbParameter()
    '     Dim ls_sql As String

    '     Dim lodb_intVisitId As OleDbParameter = New OleDbParameter()
    '     Dim lodb_dtmReceptionDate As OleDbParameter = New OleDbParameter()
    '     Dim lodb_strService As OleDbParameter = New OleDbParameter()
    '     Dim lodb_strUser As OleDbParameter = New OleDbParameter()

    '     Dim comments As String = ""

    '     lodb_intVisitId.ParameterName = "@intVisitId"
    '     lodb_intVisitId.OleDbType = OleDbType.Integer
    '     lodb_intVisitId.Value = Integer.Parse(Visita)

    '     lodb_dtmReceptionDate.ParameterName = "@dtmReceptionDate"
    '     lodb_dtmReceptionDate.OleDbType = OleDbType.Char
    '     lodb_dtmReceptionDate.Value = Format(Date.Now, "yyyyMMdd HH:mm:ss")

    '     lodb_strService.ParameterName = "@strService"
    '     lodb_strService.OleDbType = OleDbType.Char
    '     lodb_strService.Value = cadena

    '     UserName = UserName + "_m"
    '     lodb_strUser.ParameterName = "@strUser"
    '     lodb_strUser.OleDbType = OleDbType.Char
    '     lodb_strUser.Value = UserName


    '     param = oleDBcom.Parameters.Add("returnvalue", OleDbType.Integer)
    '     param.Direction = ParameterDirection.ReturnValue


    '     oleDBcom.Parameters.Add(lodb_intVisitId) '(intcontaineruniversalid) 'Id universal del contenedor)
    '     oleDBcom.Parameters.Add(lodb_dtmReceptionDate)
    '     oleDBcom.Parameters.Add(lodb_strService)
    '     oleDBcom.Parameters.Add(lodb_strUser)
    '     's_sql = "exec spUpdateHistoryContPosition ?,?,?,?,?,?, NULL ,? "
    '     's_sql = "exec intVisitId ?,?,?,? "

    '     ''''''''''''''''''''''''''''''''''''
    '     '' si el servicio es recepcion 
    '     'If cadena.IndexOf("REC") >= 0 Then

    '     '    ls_sql = "spInOutVisit"
    '     '    oleDBcom.CommandText = ls_sql
    '     '    oleDBcom.CommandType = CommandType.StoredProcedure

    '     '    oleDBcom.CommandTimeout = 0
    '     '    Try
    '     '        oleDBconnx.Open()
    '     '        ' x = param.Value
    '     '        oleDBcom.ExecuteNonQuery()
    '     '        x = 0
    '     '    Catch ex As Exception
    '     '        Dim lstr_ex As String
    '     '        lstr_ex = ex.Message
    '     '        Return -1
    '     '    Finally
    '     '        oleDBconnx.Close()
    '     '        oleDBcom.Connection.Close()
    '     '        oleDBcom.Connection.Dispose()
    '     '        oleDBconnx.Dispose()
    '     '    End Try
    '     '    ' _Error = oleDb_paramOut_ErrorCode.Value
    '     '    oleDBcom = Nothing
    '     '    oleDBconnx = Nothing
    '     'End If ' If cadena.IndexOf("REC") >= 0 Then

    '     '''''''''''''''''''''''''''''''''''''
    '     ' si el servicio es salida 
    '     'If cadena.IndexOf("ENT") >= 0 Then
    '     If cadena.Length >= 0 Then

    '         'iolecmd_comand.CommandText = ls_SQL_Command

    '         'ls_sql = "spInOutVisit"
    '         'ls_sql = "exec spInOutVisit ?,?,?,? "
    '         ' ls_sql = "execute  spInOutVisit @intVisitId = ?, @dtmReceptionDate = ?,@strService = ?, @strUser = ? "
    '         ''execute dbo.spInOutVisit  @intVisitId=1603183, @dtmReceptionDate="20190504 10:54", @strService="ENTLL", @strUser="jcadena"
    '         '' poner como texto difecto 
    '         oleDBcom.Parameters.Clear()
    '         ls_sql = "execute  spInOutVisit @intVisitId = " + lodb_intVisitId.Value.ToString() + " , @dtmReceptionDate = '" + lodb_dtmReceptionDate.Value.ToString() + "' ,@strService = '" + lodb_strService.Value.ToString() + "' , @strUser ='" + lodb_strUser.Value.ToString() + "'"

    '         oleDBcom.CommandText = ls_sql
    '         'oleDBcom.CommandType = CommandType.StoredProcedure

    '         iAdapt_comand.SelectCommand = oleDBcom
    '         Try
    '             oleDBcom.Connection.Open()
    '             iAdapt_comand.Fill(ldt_TableResult)
    '             x = 0
    '         Catch ex As Exception
    '             Dim strError As String
    '             strError = ObtenerError(ex.Message, 99999)
    '         Finally
    '             iAdapt_comand.SelectCommand.Connection.Close()
    '             oleDBcom.Connection.Close()

    '             iAdapt_comand.SelectCommand.Connection.Dispose()
    '             oleDBcom.Connection.Dispose()
    '         End Try

    '         iAdapt_comand = Nothing
    '         oleDBcom = Nothing


    '     End If 'If lengh >= 0 Then
    '     'End If 'If cadena.IndexOf("ENT") >= 0 Then

    '     ''''
    '     'iolecmd_comand.CommandText = ls_SQL_Command
    '     ''agrega parametro
    '     'iolecmd_comand.Parameters.Add("@strContainerId", OleDbType.Char)

    '     '' se pone valor al parametro
    '     'iolecmd_comand.Parameters("@strContainerId").Value = astr_ContainerId

    '     'iAdapt_comand.SelectCommand = iolecmd_comand
    '     'Try
    '     '    iolecmd_comand.Connection.Open()
    '     '    iAdapt_comand.Fill(ldt_TableResult)
    '     'Catch ex As Exception
    '     '    Dim strError As String
    '     '    strError = ObtenerError(ex.Message, 99999)
    '     'Finally
    '     '    iAdapt_comand.SelectCommand.Connection.Close()
    '     '    iolecmd_comand.Connection.Close()

    '     '    iAdapt_comand.SelectCommand.Connection.Dispose()
    '     '    iolecmd_comand.Connection.Dispose()
    '     'End Try

    '     'iAdapt_comand = Nothing
    '     'iolecmd_comand = Nothing

    '     '''''
    '     ''''''
    '     '''''''

    '     'oleDBcom.CommandTimeout = 0
    '     'Try
    '     '    oleDBconnx.Open()
    '     '    ' x = param.Value
    '     '    oleDBcom.ExecuteNonQuery()
    '     'Catch ex As Exception
    '     '    Dim lstr_ex As String
    '     '    lstr_ex = ex.Message
    '     '    Return -1
    '     'Finally
    '     '    oleDBconnx.Close()
    '     '    oleDBcom.Connection.Close()
    '     '    oleDBcom.Connection.Dispose()
    '     '    oleDBconnx.Dispose()
    '     'End Try
    '     '' _Error = oleDb_paramOut_ErrorCode.Value
    '     'oleDBcom = Nothing
    '     'oleDBconnx = Nothing

    '     Return x
    '     Return 0
    ' End Function

    ''''''''''''''''''''''''''''''
    '''''''''''''''
    <WebMethod()> _
  Public Function spInOutVisit(ByVal alng_Visita As Long, ByVal UserName As String, ByVal cadena As String) As Integer

        Dim ldtb_Result = New DataTable() ' la tabla que obtiene el resultado
        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter()
        Dim iolecmd_comand As OleDbCommand = New OleDbCommand()
        Dim ioleconx_conexion As OleDbConnection = New OleDbConnection() '' objeto de conexion que se usara para conectar 

        Dim istr_conx As String = "" ' cadena de conexion
        Dim strSQL As String = ""

        istr_conx = ConfigurationManager.ConnectionStrings("dbCalathus").ToString()
        ioleconx_conexion.ConnectionString = istr_conx
        iolecmd_comand = ioleconx_conexion.CreateCommand()

        ldtb_Result = New DataTable("Visitio")


        strSQL = "spInOutVisitWB"
        'If cadena.IndexOf("ENT") >= 0 Then
        If cadena.Length >= 0 Then

            iolecmd_comand.Parameters.Add("@intVisitId", OleDbType.Numeric)
            iolecmd_comand.Parameters("@intVisitId").Value = alng_Visita

            iolecmd_comand.Parameters.Add("@strService", OleDbType.Char)
            iolecmd_comand.Parameters("@strService").Value = cadena

            iolecmd_comand.Parameters.Add("@strUser", OleDbType.Char)

            If UserName.Length < 11 Then
                iolecmd_comand.Parameters("@strUser").Value = UserName + "_m"
            Else
                iolecmd_comand.Parameters("@strUser").Value = "m_" + UserName
            End If



            iolecmd_comand.CommandText = strSQL
            iolecmd_comand.CommandType = CommandType.StoredProcedure
            iolecmd_comand.CommandTimeout = 99999

            Try
                iAdapt_comand.SelectCommand = iolecmd_comand
                'iAdapt_comand.SelectCommand.CommandTimeout = of_getMaxTimeout()
                iAdapt_comand.Fill(ldtb_Result)
            Catch ex As Exception
                Dim strError As String = ObtenerError(ex.Message, 99999)
                strError = strError
                strError = ex.Message
            Finally
                ioleconx_conexion.Close()
            End Try




        End If 'If lengh >= 0 Then
        'End If 'If cadena.IndexOf("ENT") >= 0 Then

        ''''
        UpdateVisitStatus(alng_Visita, UserName)

        Return 0
    End Function
    ''''''''
    <WebMethod()> _
Public Function UpdateVisitStatus(ByVal alng_Visita As Long, ByVal UserName As String) As Integer

        Dim ldtb_Result = New DataTable() ' la tabla que obtiene el resultado
        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter()
        Dim iolecmd_comand As OleDbCommand = New OleDbCommand()
        Dim ioleconx_conexion As OleDbConnection = New OleDbConnection() '' objeto de conexion que se usara para conectar 

        Dim istr_conx As String = "" ' cadena de conexion
        Dim strSQL As String = ""

        istr_conx = ConfigurationManager.ConnectionStrings("dbCalathus").ToString()
        ioleconx_conexion.ConnectionString = istr_conx
        iolecmd_comand = ioleconx_conexion.CreateCommand()

        ldtb_Result = New DataTable("Visitio")


        strSQL = "spUpdateVisitStatusWB"

        iolecmd_comand.Parameters.Add("@intVisitId", OleDbType.Numeric)
        iolecmd_comand.Parameters("@intVisitId").Value = alng_Visita

        iolecmd_comand.Parameters.Add("@strStatus", OleDbType.Char)
        iolecmd_comand.Parameters("@strStatus").Value = ""

        iolecmd_comand.Parameters.Add("@strUser", OleDbType.Char)
        If UserName.Length < 11 Then
            iolecmd_comand.Parameters("@strUser").Value = UserName + "_m"
        Else
            iolecmd_comand.Parameters("@strUser").Value = "m_" + UserName
        End If



        iolecmd_comand.CommandText = strSQL
        iolecmd_comand.CommandType = CommandType.StoredProcedure
        iolecmd_comand.CommandTimeout = 99999

        Try
            iAdapt_comand.SelectCommand = iolecmd_comand
            'iAdapt_comand.SelectCommand.CommandTimeout = of_getMaxTimeout()
            iAdapt_comand.Fill(ldtb_Result)
        Catch ex As Exception
            Dim strError As String = ObtenerError(ex.Message, 99999)
            strError = strError
            strError = ex.Message
        Finally
            ioleconx_conexion.Close()
        End Try


        Return 0
    End Function
    '''''''''''''''''''''''''
    <WebMethod()> _
    Public Function spUpdateVisitStatusWB(ByVal alng_Visita As Long, ByVal UserName As String, ByVal cadena As String) As Integer

        Dim ldtb_Result = New DataTable() ' la tabla que obtiene el resultado
        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter()
        Dim iolecmd_comand As OleDbCommand = New OleDbCommand()
        Dim ioleconx_conexion As OleDbConnection = New OleDbConnection() '' objeto de conexion que se usara para conectar 

        Dim istr_conx As String = "" ' cadena de conexion
        Dim strSQL As String = ""

        istr_conx = ConfigurationManager.ConnectionStrings("dbCalathus").ToString()
        ioleconx_conexion.ConnectionString = istr_conx
        iolecmd_comand = ioleconx_conexion.CreateCommand()

        ldtb_Result = New DataTable("Visitio")


        strSQL = "spInOutVisitWB"
        'If cadena.IndexOf("ENT") >= 0 Then
        If cadena.Length >= 0 Then

            iolecmd_comand.Parameters.Add("@intVisitId", OleDbType.Numeric)
            iolecmd_comand.Parameters("@intVisitId").Value = alng_Visita

            iolecmd_comand.Parameters.Add("@strService", OleDbType.Numeric)
            iolecmd_comand.Parameters("@strService").Value = cadena

            iolecmd_comand.Parameters.Add("@strUser", OleDbType.Numeric)

            If UserName.Length < 11 Then
                iolecmd_comand.Parameters("@strUser").Value = UserName + "_m"
            Else
                iolecmd_comand.Parameters("@strUser").Value = "_m" + UserName
            End If



            iolecmd_comand.CommandText = strSQL
            iolecmd_comand.CommandType = CommandType.StoredProcedure
            iolecmd_comand.CommandTimeout = 99999

            Try
                iAdapt_comand.SelectCommand = iolecmd_comand
                'iAdapt_comand.SelectCommand.CommandTimeout = of_getMaxTimeout()
                iAdapt_comand.Fill(ldtb_Result)
            Catch ex As Exception
                Dim strError As String = ObtenerError(ex.Message, 99999)
                strError = strError
                strError = ex.Message
            Finally
                ioleconx_conexion.Close()
            End Try




        End If 'If lengh >= 0 Then
        'End If 'If cadena.IndexOf("ENT") >= 0 Then

  
        Return 0
    End Function
    ''''''''''''
    ''
    ''
    '''''''''''''''
    <WebMethod()> _
    Public Function ObtenerItemsVisitaDichLlenos(ByVal prmint_VisitId As Integer, ByVal prmstr_VisitPlate As String) As DataTable

        '-----------------------------
        Dim oleDBconnx As OleDbConnection
        Dim oleDBcom As OleDbCommand
        Dim lint_Visitnumber As Long
        Dim lstr_visitplate As String
        Dim strconx As String
        Dim lint_universal As Integer


        Dim dfind_dataadapter As OleDbDataAdapter
        Dim dfind_table As DataTable = New DataTable()


        '----------------------------------

        'variables locales
        Dim ls_sqlcom As String
        Dim ls_strContainerId As String

        Dim li_poslength As Integer
        Dim ls_Block As String
        Dim ls_Row As String
        Dim ls_Bay As String
        Dim ls_Stow As String
        Dim li_bSuccess As Integer = 0
        Dim ls_comments As String

        'declaracion de variables de base de datos
        Dim lodb_VisitNumber As OleDbParameter = New OleDbParameter()
        Dim lodb_VisitPlate As OleDbParameter = New OleDbParameter()

        Dim lodb_dataReader As OleDbDataReader

        Dim DataResult As DataTable = New Data.DataTable() 'DataSet = New DataSet()
        Dim adapter As OleDbDataAdapter

        Dim llng_Timeout As Long = 0
        Dim lint_tries As Integer = 0
        Dim lint_counterTrys As Integer = 0
        Dim lint_HasError As Integer = 0
        Dim lint_IsTimeError As Integer = 0

        Dim strError As String

        ' obtener el  valor de time out 
        llng_Timeout = of_getTimeoutSearch()

        ' obteneer la cantida de intentos 
        lint_tries = of_getSearchTries()


        ' variables locales, tendran los valores de los controles
        lstr_visitplate = prmstr_VisitPlate.Trim()

        lstr_visitplate = lstr_visitplate.ToUpper()
        'obtencion de la bahia ,bloque  y estiba 

        lint_Visitnumber = prmint_VisitId



    

       
        '' iniciar ciclo 
        Do


            strError = ""
            lodb_VisitNumber = New OleDbParameter()
            lodb_VisitPlate = New OleDbParameter()

            oleDBconnx = New OleDbConnection()
            oleDBconnx.ConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString

            oleDBcom = oleDBconnx.CreateCommand

            lodb_VisitNumber.OleDbType = OleDbType.Integer
            lodb_VisitNumber.ParameterName = "@aintVisitId"
            lodb_VisitNumber.Value = lint_Visitnumber

            lodb_VisitPlate.OleDbType = OleDbType.VarChar
            lodb_VisitPlate.ParameterName = "@astrVisitPlate"
            lodb_VisitPlate.Value = 20
            lodb_VisitPlate.Value = lstr_visitplate

            ls_sqlcom = "spGetVItemsDischYardFull"

            oleDBcom.Parameters.Add(lodb_VisitNumber)
            oleDBcom.Parameters.Add(lodb_VisitPlate)

            oleDBcom.CommandType = CommandType.StoredProcedure
            oleDBcom.CommandText = ls_sqlcom

            'oleDBcom.CommandTimeout = 7

            ' si hay timeout, del archivo ponerlo 
            If llng_Timeout > 0 Then
                oleDBcom.CommandTimeout = llng_Timeout
            Else
                oleDBcom.CommandTimeout = 0
            End If

            DataResult = New Data.DataTable() 'DataSet = New DataSet()
            DataResult.TableName = "TrearDatos"

            adapter = New OleDbDataAdapter(oleDBcom)

            Try
                oleDBconnx.Open()
                'oleDBcom.ExecuteNonQuery()
                adapter.Fill(DataResult)


                lint_HasError = 0

                'se termino la consulta sin llegar a timeout
                lint_counterTrys = lint_tries


            Catch ex As Exception

                strError = ObtenerError(ex.Message, 99999)


                If strError.ToLower.Contains("excedio") = True Or strError.ToLower.Contains("tiempo") = True Or strError.ToLower.Contains("time") = True Then
                    lint_IsTimeError = 1
                Else
                    lint_IsTimeError = 0
                    'hubo un error no era de timeout. ya forzar el ciclos
                    lint_counterTrys = lint_tries
                End If

                DataResult = dt_RetrieveErrorTable(strError)

                'Return dt_RetrieveErrorTable(strError)


            Finally
                If oleDBconnx IsNot Nothing Then
                    oleDBconnx.Close()
                    oleDBconnx.Dispose()
                End If

                oleDBconnx = Nothing

            End Try

            lint_counterTrys = lint_counterTrys + 1
            ' mientras el contador sea menor que el limite del archivo 

        Loop While lint_counterTrys < lint_tries

        Return DataResult

    End Function


    '''''''''''
    '''''''''''''''''''''

    '''''''''''''''
    <WebMethod()> _
    Public Function ObtenerItemsVisitaChargeConts(ByVal prmint_VisitId As Integer, ByVal prmstr_VisitPlate As String) As DataTable

        '-----------------------------
        Dim oleDBconnx As OleDbConnection
        Dim oleDBcom As OleDbCommand
        Dim lint_Visitnumber As Long
        Dim lstr_visitplate As String
        Dim strconx As String
        Dim lint_universal As Integer


        Dim dfind_dataadapter As OleDbDataAdapter
        Dim dfind_table As DataTable = New DataTable()
        '-------------------------

        Dim oldCulture = Thread.CurrentThread.CurrentCulture
        Dim cultureToNormalize As CultureInfo

        Using international As RegistryKey = Registry.CurrentUser.OpenSubKey("Control Panel\\International", False)

            Dim userDefaultCulture = international.GetValue("LocaleName").ToString()
            cultureToNormalize = New CultureInfo(userDefaultCulture, False)

        End Using
        Thread.CurrentThread.CurrentCulture = cultureToNormalize
        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("es-MX")
        Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("es-MX")

            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

            oleDBconnx = New OleDbConnection()
            oleDBconnx.ConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString

            oleDBcom = oleDBconnx.CreateCommand

            '----------------------------------

            'variables locales
            Dim ls_sqlcom As String
            Dim ls_strContainerId As String

            Dim li_poslength As Integer
            Dim ls_Block As String
            Dim ls_Row As String
            Dim ls_Bay As String
            Dim ls_Stow As String
            Dim li_bSuccess As Integer = 0
            Dim ls_comments As String

            'declaracion de variables de base de datos
            Dim lodb_VisitNumber As OleDbParameter = New OleDbParameter()
            Dim lodb_VisitPlate As OleDbParameter = New OleDbParameter()

            Dim lodb_dataReader As OleDbDataReader
            Dim DataResult As DataTable = New Data.DataTable() 'DataSet = New DataSet()    Dim DataResult As DataTable = New Data.DataTable() 'DataSet = New DataSet()
            Dim adapter As OleDbDataAdapter = New OleDbDataAdapter(oleDBcom)

            ' variables locales, tendran los valores de los controles
            lstr_visitplate = prmstr_VisitPlate.Trim()

            lstr_visitplate = lstr_visitplate.ToUpper()
            'obtencion de la bahia ,bloque  y estiba 

            lint_Visitnumber = prmint_VisitId


            Dim llng_Timeout As Long = 0
            Dim lint_tries As Integer = 0
            Dim lint_counterTrys As Integer = 0
            Dim lint_HasError As Integer = 0
            Dim lint_IsTimeError As Integer = 0

            Dim strError As String

            ' obtener el  valor de time out 
            llng_Timeout = of_getTimeoutSearch()
            ' obteneer la cantida de intentos 
            lint_tries = of_getSearchTries()



        Dim lstr_x As String = HttpUtility.HtmlDecode("x")


            'CICLO
            Do

                dfind_table = New DataTable()

                lodb_VisitNumber = New OleDbParameter()
                lodb_VisitPlate = New OleDbParameter()


                oleDBconnx = New OleDbConnection()
                oleDBconnx.ConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString

                oleDBcom = oleDBconnx.CreateCommand
                strError = ""
                If llng_Timeout > 0 Then
                oleDBcom.CommandTimeout = llng_Timeout
            Else
                oleDBcom.CommandTimeout = 0

            End If

                lodb_VisitNumber.OleDbType = OleDbType.Integer
                lodb_VisitNumber.ParameterName = "@aintVisitId"
                lodb_VisitNumber.Value = lint_Visitnumber

                lodb_VisitPlate.OleDbType = OleDbType.VarChar
                lodb_VisitPlate.ParameterName = "@astrVisitPlate"
                lodb_VisitPlate.Value = 20
                lodb_VisitPlate.Value = lstr_visitplate

                ls_sqlcom = "spGetVisitItemsLoadYard"

                oleDBcom.Parameters.Add(lodb_VisitNumber)
                oleDBcom.Parameters.Add(lodb_VisitPlate)

                oleDBcom.CommandType = CommandType.StoredProcedure
                oleDBcom.CommandText = ls_sqlcom
                'oleDBcom.CommandTimeout = 7

                DataResult = New Data.DataTable() 'DataSet = New DataSet()
                DataResult.TableName = "TrearDatos"

                adapter = New OleDbDataAdapter(oleDBcom)

                Try
                    oleDBconnx.Open()
                    'oleDBcom.ExecuteNonQuery()
                    adapter.Fill(DataResult)

                    lint_HasError = 0
                    'se termino la consulta sin llegar a timeout
                    lint_counterTrys = lint_tries

                Catch ex As Exception

                    strError = ObtenerError(ex.Message, 99999)

                    If strError.ToLower.Contains("excedio") = True Or strError.ToLower.Contains("tiempo") = True Or strError.ToLower.Contains("time") = True Then
                        lint_IsTimeError = 1
                    Else
                        lint_IsTimeError = 0
                        'hubo un error no era de timeout. ya forzar el ciclos
                        lint_counterTrys = lint_tries
                    End If

                    DataResult = dt_RetrieveErrorTable(strError)


                    ' Return dt_RetrieveErrorTable(strError)

                Finally

                    If oleDBconnx IsNot Nothing Then
                        oleDBconnx.Close()
                        oleDBconnx.Dispose()
                        oleDBconnx = Nothing
                    End If

                End Try

                lint_counterTrys = lint_counterTrys + 1
                ' mientras el contador sea menor que el limite del archivo 

            Loop While lint_counterTrys < lint_tries

        'Dim ldt_convTable As DataTable = New DataTable("result2")

        'CopyTable(DataResult, ldt_convTable)
        'ldt_convTable.TableName = "result2"
        Return DataResult

    End Function


    ''''''''''''''
    <WebMethod()> _
    Public Function ChargeContainers(ByVal lint_visit As Integer, ByVal lstr_service As String, ByVal adtb_tablecontlist As DataTable, ByVal lstr_username As String) As String


        Dim oleDBconnx As OleDbConnection
        Dim oleDBcom As OleDbCommand
        oleDBcom = New OleDbCommand()
        oleDBconnx = New OleDbConnection()
        Dim strconx As String

        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando
        Dim ldt_TableResult As DataTable = New DataTable("Result")

        strconx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        oleDBconnx.ConnectionString = strconx
        oleDBcom = oleDBconnx.CreateCommand
        '------------------------------
        Dim lstr_ValueChange As String
        'Declaracio de variables de posicion 
        Dim lstr_bahia As String, lstr_fila As String, lstr_bloque As String, lstr_nivel As String
        Dim lint_sosid As Integer, lint_servqueu As Integer

        lstr_ValueChange = lstr_bahia
        lstr_bahia = lstr_fila
        '-- cambio valor
        lstr_fila = lstr_ValueChange
        '----------------------------------
        Dim oleDb_param As OleDbParameter = New OleDbParameter()
        Dim ls_sql As String

        Dim aintVisit As OleDbParameter = New OleDbParameter()
        Dim aiduniversal As OleDbParameter = New OleDbParameter()
        Dim astrservice As OleDbParameter = New OleDbParameter()
        Dim astrposition As OleDbParameter = New OleDbParameter()
        Dim Block As OleDbParameter = New OleDbParameter()
        Dim Row As OleDbParameter = New OleDbParameter()
        Dim Bay As OleDbParameter = New OleDbParameter()
        Dim Stow As OleDbParameter = New OleDbParameter()

        Dim aintSOrderId As OleDbParameter = New OleDbParameter()
        Dim aintServQueu As OleDbParameter = New OleDbParameter()
        Dim dtmprocessdate As OleDbParameter = New OleDbParameter()
        Dim astruser As OleDbParameter = New OleDbParameter()
        Dim lstr_universal As String
        Dim lstr_position As String
        Dim lint_universal As Integer
        Dim lint_counter As Integer
        Dim lstr_serviceorderid As String
        Dim lstr_tempstring As String

        '' validar la tabla 
        ''renglones
        If adtb_tablecontlist.Rows.Count = 0 Then
            Return "No hay contenedores"
        End If

        'columnas
        If adtb_tablecontlist.Columns.Count = 0 Then
            Return "No hay columnas"
        End If


        ' validar la indormacion
        lstr_tempstring = of_checkInfo_Charge(lint_visit, lstr_service, adtb_tablecontlist)

        ' SI MARCO ERROR 
        If lstr_tempstring.Length > 1 Then
            Return "-1-" + lstr_tempstring
        End If

        '' try del ciclo
        Try

            '''''''''''''''''''''''''''''''
            For Each itemrow In adtb_tablecontlist.Rows

                'reiniciar parametro 
                oleDBcom.Parameters.Clear()

                ' incrementar contador 
                lint_counter = lint_counter + 1
                'reiniciar los valores  de lectura en cada columna 
                lstr_universal = ""
                lstr_position = ""
                lint_universal = 0
                lstr_bloque = ""
                lstr_fila = ""
                lstr_bahia = ""
                lstr_nivel = ""
                lint_sosid = 0
                lstr_serviceorderid = ""
                lint_servqueu = 0

                'inicializar adapater
                iAdapt_comand = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando

                'conexion
                strconx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
                oleDBconnx.ConnectionString = strconx
                oleDBcom = oleDBconnx.CreateCommand

                aintVisit.ParameterName = "@aintVisit"
                aintVisit.OleDbType = OleDbType.Integer
                aintVisit.Value = lint_visit


                'obtener el universal 
                lstr_universal = itemrow("intContainerUniversalId")
                If Integer.TryParse(lstr_universal, lint_universal) = False Then
                    lint_universal = 0
                End If

                aiduniversal.ParameterName = "@aiduniversal"
                aiduniversal.OleDbType = OleDbType.Integer
                aiduniversal.Value = lint_universal 'valor("lstrcontainerinvyardpositionid")

                'obtener el servicio 
                lstr_service = itemrow("strServiceId").ToString()

                astrservice.ParameterName = "@astrservice"
                astrservice.OleDbType = OleDbType.Char
                astrservice.Value = lstr_service 'Mid(valor("strContainerInvYardPositionId"), 1, 2) 'bloque

                'obtener la posicion 
                lstr_position = itemrow("strLocation").ToString()

                astrposition.ParameterName = "@astrposition"
                astrposition.OleDbType = OleDbType.Char
                'astrposition.Value = lstr_position 'Mid(valor("strContainerInvYardPositionId"), 3, 2) 'baia
                astrposition.Value = ""

                ' obtner el bloque 
                lstr_bloque = itemrow("strBlock").ToString()

                Block.ParameterName = "@Block"
                Block.OleDbType = OleDbType.Char
                'Block.Value = lstr_bloque 'Mid(valor("strContainerInvYardPositionId"), 5, 1) 'fila
                Block.Value = ""

                'obtener fila 
                lstr_fila = itemrow("strRow").ToString()

                Row.ParameterName = "@Row"
                Row.OleDbType = OleDbType.Char
                ''Row.Value = lstr_fila 'Mid(valor("strContainerInvYardPositionId"), 6, 1) 'nivel
                Row.Value = ""

                'obtener bahia 
                lstr_bahia = itemrow("strBay").ToString()

                Bay.ParameterName = "@Bay"
                Bay.OleDbType = OleDbType.Char
                ' Bay.Value = lstr_bahia
                Bay.Value = ""

                'obtener estiba 
                lstr_nivel = itemrow("strStow").ToString()

                Stow.ParameterName = "@Stow"
                Stow.OleDbType = OleDbType.Char
                'Stow.Value = lstr_nivel
                Stow.Value = ""

                'obtener el numero de maniobra como numero 
                lstr_serviceorderid = itemrow("intServiceOrderId").ToString()

                If Integer.TryParse(lstr_serviceorderid, lint_sosid) = False Then
                    lint_sosid = 0
                End If

                'maniobra
                aintSOrderId.ParameterName = "@aintSOrderId"
                aintSOrderId.OleDbType = OleDbType.Integer
                aintSOrderId.Value = lint_sosid
                'aintSOrderId.Value = ""

                'obtener el serviceque
                lstr_tempstring = itemrow("intServiceQueId").ToString()
                If Integer.TryParse(lstr_tempstring, lint_servqueu) = False Then
                    lint_servqueu = 0
                End If

                aintServQueu.ParameterName = "@aintServQueu"
                aintServQueu.OleDbType = OleDbType.Integer
                aintServQueu.Value = lint_servqueu

                dtmprocessdate.ParameterName = "@dtmprocessdate"
                dtmprocessdate.OleDbType = OleDbType.Char
                dtmprocessdate.Value = Format(Date.Now, "yyyyMMdd HH:mm")

                astruser.ParameterName = "@astruser"
                astruser.OleDbType = OleDbType.Char
                astruser.Value = lstr_username

                ls_sql = "spProcessVisitQueue"

                oleDBcom.CommandText = ls_sql
                'oleDBcom.CommandType = CommandType.StoredProcedure

                oleDBcom.Parameters.Add(aintVisit)
                oleDBcom.Parameters.Add(aiduniversal)
                oleDBcom.Parameters.Add(astrservice)
                oleDBcom.Parameters.Add(astrposition)
                oleDBcom.Parameters.Add(Block)
                oleDBcom.Parameters.Add(Row)
                oleDBcom.Parameters.Add(Bay)
                oleDBcom.Parameters.Add(Stow)
                oleDBcom.Parameters.Add(aintSOrderId)
                oleDBcom.Parameters.Add(aintServQueu)
                oleDBcom.Parameters.Add(dtmprocessdate)
                oleDBcom.Parameters.Add(astruser)

                oleDBcom.Parameters.Clear()
                oleDBcom.CommandType = CommandType.Text

                ls_sql = " execute spProcessVisitQueue  @aintVisit=" + aintVisit.Value.ToString() + ", @aiduniversal=" + aiduniversal.Value.ToString() + ", @astrservice='" + astrservice.Value.ToString() + "' , @astrposition= '" + astrposition.Value.ToString() + "', @Block='" + Block.Value.ToString() + "', @Row='" + Row.Value.ToString() + "' , @Bay='" + Bay.Value.ToString() + "', @Stow='" + Stow.Value.ToString() + "', @aintSOrderId=" + aintSOrderId.Value.ToString() + ", @aintServQueu=" + aintServQueu.Value.ToString() + ", @dtmprocessdate='" + dtmprocessdate.Value.ToString() + "', @astruser='" + astruser.Value.ToString() + "'"
                oleDBcom.CommandText = ls_sql

                oleDBcom.CommandTimeout = 0


                iAdapt_comand.SelectCommand = oleDBcom
                Try
                    oleDBcom.Connection.Open()
                    iAdapt_comand.Fill(ldt_TableResult)

                Catch ex As Exception
                    Dim strError As String
                    strError = ObtenerError(ex.Message, 99999)
                    Return "error::+" + strError + ex.Message

                Finally
                    iAdapt_comand.SelectCommand.Connection.Close()
                    oleDBcom.Connection.Close()

                    iAdapt_comand.SelectCommand.Connection.Dispose()
                    oleDBcom.Connection.Dispose()
                End Try

                ' iAdapt_comand = Nothing
                ' oleDBcom = Nothing



                'Try
                '    oleDBcom.Connection.Open()
                '    oleDBcom.ExecuteNonQuery()

                'Catch ex As Exception

                'Finally
                '    oleDBcom.Connection.Close()
                '    oleDBconnx.Close()
                '    oleDBcom.Parameters.Clear()

                '    oleDBcom.Connection.Dispose()
                '    oleDBconnx.Dispose()

                'End Try


            Next ' For Each itemrow In adtb_tablecontlist.Rows


        Catch ex As Exception

            Dim lstr_error As String
            lstr_error = ex.Message
            Return "0:" + lstr_error
        End Try '' fin try ciclo 
        'Return lint_counter.ToString()

        Return ""
    End Function

    ''''
    ''''' carga contenedores
    <WebMethod()> _
    Public Function DischargeContainers(ByVal lint_visit As Integer, ByVal lstr_service As String, ByVal adtb_tablecontlist As DataTable, ByVal lstr_username As String) As String


        Dim oleDBconnx As OleDbConnection
        Dim oleDBcom As OleDbCommand
        oleDBcom = New OleDbCommand()
        oleDBconnx = New OleDbConnection()
        Dim strconx As String

        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando
        Dim ldt_TableResult As DataTable = New DataTable("Result")

        strconx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        oleDBconnx.ConnectionString = strconx
        oleDBcom = oleDBconnx.CreateCommand
        '------------------------------
        Dim lstr_ValueChange As String
        'Declaracio de variables de posicion 
        Dim lstr_bahia As String, lstr_fila As String, lstr_bloque As String, lstr_nivel As String
        Dim lint_sosid As Integer, lint_servqueu As Integer

        lstr_ValueChange = lstr_bahia
        lstr_bahia = lstr_fila
        '-- cambio valor
        lstr_fila = lstr_ValueChange
        '----------------------------------
        Dim oleDb_param As OleDbParameter = New OleDbParameter()
        Dim ls_sql As String

        Dim aintVisit As OleDbParameter = New OleDbParameter()
        Dim aiduniversal As OleDbParameter = New OleDbParameter()
        Dim astrservice As OleDbParameter = New OleDbParameter()
        Dim astrposition As OleDbParameter = New OleDbParameter()
        Dim Block As OleDbParameter = New OleDbParameter()
        Dim Row As OleDbParameter = New OleDbParameter()
        Dim Bay As OleDbParameter = New OleDbParameter()
        Dim Stow As OleDbParameter = New OleDbParameter()

        Dim aintSOrderId As OleDbParameter = New OleDbParameter()
        Dim aintServQueu As OleDbParameter = New OleDbParameter()
        Dim dtmprocessdate As OleDbParameter = New OleDbParameter()
        Dim astruser As OleDbParameter = New OleDbParameter()
        Dim lstr_universal As String
        Dim lstr_position As String
        Dim lint_universal As Integer
        Dim lint_counter As Integer
        Dim lstr_serviceorderid As String
        Dim lstr_tempstring As String

        '' validar la tabla 
        ''renglones
        If adtb_tablecontlist.Rows.Count = 0 Then
            Return "No hay contenedores"
        End If

        'columnas
        If adtb_tablecontlist.Columns.Count = 0 Then
            Return "No hay columnas"
        End If


        ' validar la indormacion
        lstr_tempstring = of_checkInfo_Discharge(lint_visit, lstr_service, adtb_tablecontlist)

        '' try del ciclo
        Try

            '''''''''''''''''''''''''''''''
            For Each itemrow In adtb_tablecontlist.Rows

                'reiniciar parametro 
                oleDBcom.Parameters.Clear()

                ' incrementar contador 
                lint_counter = lint_counter + 1
                'reiniciar los valores  de lectura en cada columna 
                lstr_universal = ""
                lstr_position = ""
                lint_universal = 0
                lstr_bloque = ""
                lstr_fila = ""
                lstr_bahia = ""
                lstr_nivel = ""
                lint_sosid = 0
                lstr_serviceorderid = ""
                lint_servqueu = 0

                'inicializar adapater
                iAdapt_comand = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando

                'conexion
                strconx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
                oleDBconnx.ConnectionString = strconx
                oleDBcom = oleDBconnx.CreateCommand

                aintVisit.ParameterName = "@aintVisit"
                aintVisit.OleDbType = OleDbType.Integer
                aintVisit.Value = lint_visit


                'obtener el universal 
                lstr_universal = itemrow("intContainerUniversalId")
                If Integer.TryParse(lstr_universal, lint_universal) = False Then
                    lint_universal = 0
                End If

                aiduniversal.ParameterName = "@aiduniversal"
                aiduniversal.OleDbType = OleDbType.Integer
                aiduniversal.Value = lint_universal 'valor("lstrcontainerinvyardpositionid")

                'obtener el servicio 
                lstr_service = itemrow("strServiceId").ToString()

                astrservice.ParameterName = "@astrservice"
                astrservice.OleDbType = OleDbType.Char
                astrservice.Value = lstr_service 'Mid(valor("strContainerInvYardPositionId"), 1, 2) 'bloque

                'obtener la posicion 
                lstr_position = itemrow("strLocation").ToString()

                astrposition.ParameterName = "@astrposition"
                astrposition.OleDbType = OleDbType.Char
                astrposition.Value = lstr_position 'Mid(valor("strContainerInvYardPositionId"), 3, 2) 'baia

                ' obtner el bloque 
                lstr_bloque = itemrow("strBlock").ToString()

                Block.ParameterName = "@Block"
                Block.OleDbType = OleDbType.Char
                Block.Value = lstr_bloque 'Mid(valor("strContainerInvYardPositionId"), 5, 1) 'fila

                'obtener fila 
                lstr_fila = itemrow("strRow").ToString()

                Row.ParameterName = "@Row"
                Row.OleDbType = OleDbType.Char
                Row.Value = lstr_fila 'Mid(valor("strContainerInvYardPositionId"), 6, 1) 'nivel

                'obtener bahia 
                lstr_bahia = itemrow("strBay").ToString()

                Bay.ParameterName = "@Bay"
                Bay.OleDbType = OleDbType.Char
                Bay.Value = lstr_bahia

                'obtener estiba 
                lstr_nivel = itemrow("strStow").ToString()

                Stow.ParameterName = "@Stow"
                Stow.OleDbType = OleDbType.Char
                Stow.Value = lstr_nivel

                'obtener el numero de maniobra como numero 
                lstr_serviceorderid = itemrow("intServiceOrderId").ToString()

                If Integer.TryParse(lstr_serviceorderid, lint_sosid) = False Then
                    lint_sosid = 0
                End If

                'maniobra
                aintSOrderId.ParameterName = "@aintSOrderId"
                aintSOrderId.OleDbType = OleDbType.Integer
                aintSOrderId.Value = lint_sosid

                'obtener el serviceque
                lstr_tempstring = itemrow("intServiceQueId").ToString()
                If Integer.TryParse(lstr_tempstring, lint_servqueu) = False Then
                    lint_servqueu = 0
                End If

                aintServQueu.ParameterName = "@aintServQueu"
                aintServQueu.OleDbType = OleDbType.Integer
                aintServQueu.Value = lint_servqueu

                dtmprocessdate.ParameterName = "@dtmprocessdate"
                dtmprocessdate.OleDbType = OleDbType.Char
                dtmprocessdate.Value = Format(Date.Now, "yyyyMMdd HH:mm")

                astruser.ParameterName = "@astruser"
                astruser.OleDbType = OleDbType.Char
                astruser.Value = lstr_username

                ls_sql = "spProcessVisitQueue"

                oleDBcom.CommandText = ls_sql
                'oleDBcom.CommandType = CommandType.StoredProcedure

                oleDBcom.Parameters.Add(aintVisit)
                oleDBcom.Parameters.Add(aiduniversal)
                oleDBcom.Parameters.Add(astrservice)
                oleDBcom.Parameters.Add(astrposition)
                oleDBcom.Parameters.Add(Block)
                oleDBcom.Parameters.Add(Row)
                oleDBcom.Parameters.Add(Bay)
                oleDBcom.Parameters.Add(Stow)
                oleDBcom.Parameters.Add(aintSOrderId)
                oleDBcom.Parameters.Add(aintServQueu)
                oleDBcom.Parameters.Add(dtmprocessdate)
                oleDBcom.Parameters.Add(astruser)

                oleDBcom.Parameters.Clear()
                oleDBcom.CommandType = CommandType.Text

                ls_sql = " execute spProcessVisitQueue  @aintVisit=" + aintVisit.Value.ToString() + ", @aiduniversal=" + aiduniversal.Value.ToString() + ", @astrservice='" + astrservice.Value.ToString() + "' , @astrposition= '" + astrposition.Value.ToString() + "', @Block='" + Block.Value.ToString() + "', @Row='" + Row.Value.ToString() + "' , @Bay='" + Bay.Value.ToString() + "', @Stow='" + Stow.Value.ToString() + "', @aintSOrderId=" + aintSOrderId.Value.ToString() + ", @aintServQueu=" + aintServQueu.Value.ToString() + ", @dtmprocessdate='" + dtmprocessdate.Value.ToString() + "', @astruser='" + astruser.Value.ToString() + "'"
                oleDBcom.CommandText = ls_sql

                oleDBcom.CommandTimeout = 0


                iAdapt_comand.SelectCommand = oleDBcom
                Try
                    oleDBcom.Connection.Open()
                    iAdapt_comand.Fill(ldt_TableResult)

                Catch ex As Exception
                    Dim strError As String
                    strError = ObtenerError(ex.Message, 99999)
                    Return "error::+" + strError + ex.Message

                Finally
                    iAdapt_comand.SelectCommand.Connection.Close()
                    oleDBcom.Connection.Close()

                    iAdapt_comand.SelectCommand.Connection.Dispose()
                    oleDBcom.Connection.Dispose()
                End Try

                ' iAdapt_comand = Nothing
                ' oleDBcom = Nothing



                'Try
                '    oleDBcom.Connection.Open()
                '    oleDBcom.ExecuteNonQuery()

                'Catch ex As Exception

                'Finally
                '    oleDBcom.Connection.Close()
                '    oleDBconnx.Close()
                '    oleDBcom.Parameters.Clear()

                '    oleDBcom.Connection.Dispose()
                '    oleDBconnx.Dispose()

                'End Try


            Next ' For Each itemrow In adtb_tablecontlist.Rows


        Catch ex As Exception

            Dim lstr_error As String
            lstr_error = ex.Message
            Return lstr_error
        End Try '' fin try ciclo 
        'Return lint_counter.ToString()

        Return ""
    End Function

    ''''''''''''''''''
    '''''''''''''''''''

    Public Function of_checkInfo_Discharge(ByVal aint_visit As Integer, ByVal astr_service As String, ByVal adtb_tablecontlist As DataTable) As String

        'contador 
        Dim lint_counter As Integer

        'variables universal 
        Dim lstr_universal As String
        Dim lstr_position As String
        Dim lint_universal As Integer
        Dim lstr_bloque As String
        Dim lstr_fila As String
        Dim lstr_bahia As String
        Dim lstr_nivel As String
        Dim lint_sosid As Integer
        Dim lstr_serviceorderid As String
        Dim lint_servqueu As Integer
        Dim lstr_service As String
        Dim lstr_tempstring As String



        lint_counter = 0

        ' revisar el id de la visita 
        If aint_visit = 0 Then
            Return "Visita invalida"
        End If

        'validar nombre del servicio 
        If astr_service.Length = 0 Then
            Return "servicio invalido"

        End If


        'validacion de la tabla
        Try

            If adtb_tablecontlist.Rows.Count = 0 Then
                Return "No hay lista de contenedores"
            End If

            If adtb_tablecontlist.Columns.Count = 0 Then
                Return "No hay columnas de informacion"
            End If

            '''''''''''''''''''''''''''''''
            For Each itemrow In adtb_tablecontlist.Rows


                ' incrementar contador 
                lint_counter = lint_counter + 1
                'reiniciar los valores  de lectura en cada columna 
                lstr_universal = ""
                lstr_position = ""
                lint_universal = 0
                lstr_bloque = ""
                lstr_fila = ""
                lstr_bahia = ""
                lstr_nivel = ""
                lint_sosid = 0
                lstr_serviceorderid = ""
                lint_servqueu = 0

                'obtener el universal 
                lstr_universal = itemrow("intContainerUniversalId")
                If Integer.TryParse(lstr_universal, lint_universal) = False Then
                    lint_universal = 0
                End If

                ' si el universal es 0, marcar error
                If lint_universal = 0 Then
                    Return " El item (" + lint_counter.ToString() + ") no tiene universal "
                End If
                'obtener el servicio 
                lstr_service = itemrow("strServiceId").ToString()
                If lstr_service.Length = 0 Then
                    Return "No tiene servicio el item " + lint_counter.ToString()
                End If


                'obtener la posicion 
                lstr_position = itemrow("strLocation").ToString()
                If lstr_position.Length = 0 Then
                    Return "No se capturo la posicion en el item " + lint_counter.ToString()
                End If



                'obtener el numero de maniobra como numero 
                lstr_serviceorderid = itemrow("intServiceOrderId").ToString()

                If Integer.TryParse(lstr_serviceorderid, lint_sosid) = False Then
                    lint_sosid = 0
                    Return "No tiene maniobra el item:" + lint_counter.ToString()
                End If

                'obtener el id de serviceque
                lstr_tempstring = ""

                lstr_tempstring = itemrow("intServiceQueId").ToString()
                If Integer.TryParse(lstr_tempstring, lint_servqueu) = False Then
                    lint_servqueu = 0
                    Return "No tiene servicequeue el item:" + lint_counter.ToString()
                End If

            Next ' For Each itemrow In adtb_tablecontlist.Rows


        Catch ex As Exception

            Dim lstr_error As String
            lstr_error = ex.Message
            Return lstr_error

        End Try ' fin del try 


        Return ""
    End Function
    ''''''''''''
    ''''''''''''

    Public Function of_checkInfo_Charge(ByVal aint_visit As Integer, ByVal astr_service As String, ByVal adtb_tablecontlist As DataTable) As String

        'contador 
        Dim lint_counter As Integer

        'variables universal 
        Dim lstr_universal As String
        Dim lstr_position As String
        Dim lint_universal As Integer
        Dim lstr_bloque As String
        Dim lstr_fila As String
        Dim lstr_bahia As String
        Dim lstr_nivel As String
        Dim lint_sosid As Integer
        Dim lstr_serviceorderid As String
        Dim lint_servqueu As Integer
        Dim lstr_service As String
        Dim lstr_tempstring As String



        lint_counter = 0

        ' revisar el id de la visita 
        If aint_visit = 0 Then
            Return "Visita invalida"
        End If

        'validar nombre del servicio 
        If astr_service.Length = 0 Then
            Return "servicio invalido"

        End If


        'validacion de la tabla
        Try

            If adtb_tablecontlist.Rows.Count = 0 Then
                Return "No hay lista de contenedores"
            End If

            If adtb_tablecontlist.Columns.Count = 0 Then
                Return "No hay columnas de informacion"
            End If

            '''''''''''''''''''''''''''''''
            For Each itemrow In adtb_tablecontlist.Rows


                ' incrementar contador 
                lint_counter = lint_counter + 1
                'reiniciar los valores  de lectura en cada columna 
                lstr_universal = ""
                lstr_position = ""
                lint_universal = 0
                lstr_bloque = ""
                lstr_fila = ""
                lstr_bahia = ""
                lstr_nivel = ""
                lint_sosid = 0
                lstr_serviceorderid = ""
                lint_servqueu = 0

                'obtener el universal 
                lstr_universal = itemrow("intContainerUniversalId")
                If Integer.TryParse(lstr_universal, lint_universal) = False Then
                    lint_universal = 0
                End If

                ' si el universal es 0, marcar error
                If lint_universal = 0 Then
                    Return " El item (" + lint_counter.ToString() + ") no tiene universal "
                End If
                'obtener el servicio 
                lstr_service = itemrow("strServiceId").ToString()
                If lstr_service.Length = 0 Then
                    Return "No tiene servicio el item " + lint_counter.ToString()
                End If


                'obtener la posicion 
                'lstr_position = itemrow("strLocation").ToString()
                'If lstr_position.Length = 0 Then
                '    Return "No se capturo la posicion en el item " + lint_counter.ToString()
                'End If



                'obtener el numero de maniobra como numero 
                lstr_serviceorderid = itemrow("intServiceOrderId").ToString()

                If Integer.TryParse(lstr_serviceorderid, lint_sosid) = False Then
                    lint_sosid = 0
                    Return "No tiene maniobra el item:" + lint_counter.ToString()
                End If

                'obtener el id de serviceque
                lstr_tempstring = ""

                lstr_tempstring = itemrow("intServiceQueId").ToString()
                If Integer.TryParse(lstr_tempstring, lint_servqueu) = False Then
                    lint_servqueu = 0
                    Return "No tiene servicequeue el item:" + lint_counter.ToString()
                End If

            Next ' For Each itemrow In adtb_tablecontlist.Rows


        Catch ex As Exception

            Dim lstr_error As String
            lstr_error = ex.Message
            Return lstr_error

        End Try ' fin del try 


        Return ""
    End Function
    '''''''''''''''''''''''''''
    '''''''''''''''''''''''''''


    '''''''''''''''
    <WebMethod()> _
    Public Function Search_For_Containers_Carga(ByVal VisitId As Integer) As DataTable
        '-----------------------------
        'Dim VisitId As Integer = 721372
        Dim iAdapt_comand As OleDbDataAdapter = New OleDbDataAdapter() '' Adaptador que ejecuta la tabla y el comando
        Dim iolecmd_comand As OleDbCommand '' objeto comando que se ejecutara
        Dim ioleconx_conexion As OleDbConnection = New OleDbConnection() '' objeto de conexion que se usara para conectar 
        Dim istr_conx As String '' cadena de conexion
        istr_conx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        ioleconx_conexion.ConnectionString = istr_conx
        iolecmd_comand = ioleconx_conexion.CreateCommand()
        '----------------------------------
        Dim tablafuente As New Data.DataTable("fuente")
        Dim tablaQUERY As New Data.DataTable("fuente")
        tablafuente.Columns.Add("cargado", System.Type.GetType("System.Int32"))
        tablafuente.Columns.Add("contenedor", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("tipo", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("tamaño", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("lleno", System.Type.GetType("System.Int32"))
        tablafuente.Columns.Add("linea", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("clase", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("posicion", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("fecha", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("iduniversal", System.Type.GetType("System.Int32"))
        tablafuente.Columns.Add("servicio", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("status", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("soid", System.Type.GetType("System.Int32"))
        tablafuente.Columns.Add("queueid", System.Type.GetType("System.Int32"))
        tablafuente.Columns.Add("dtmin", System.Type.GetType("System.String"))
        tablafuente.Columns.Add("dtmout", System.Type.GetType("System.String"))


        Dim ls_SQL_Command As String

        ls_SQL_Command = "SELECT 0 as cargado , SQ.intVisitId," & _
                                            "I.strContainerInvYardPositionId, " & _
                                "(CASE WHEN ( SELECT INV.blnContainerIsFull " & _
                                "FROM tblclsContainerInventory INV " & _
                                "WHERE INV.intContainerUniversalId  = SQ.intContainerUniversalId )  = 1 " & _
                                               "THEN 1 " & _
                                               "ELSE 0 " & _
                                        "END) as blnContainerIsFull, " & _
                                       "SQ.intContainerUniversalId, " & _
                                       "SV.strServiceIdentifier, " & _
                                       "SQ.strContainerId, " & _
                                       "T.strContainerTypeIdentifier, " & _
                                       "S.strContainerSizeIdentifier, " & _
                                       "SQ.intServiceOrderId, " & _
                                       "SV.strServiceName, " & _
                                       "SQ.dtmServiceQueuStartDate, " & _
                                       "SQ.dtmServiceQueuExecDate, " & _
                                       "SQ.intServiceId, " & _
                                       "SQ.intServiceQueuId, " & _
                                       "SV.strServiceIdentifier, " & _
                                       "(CASE ISNULL(I.intContainerUniversalId,0) " & _
                                       "        WHEN 0 THEN 'SIN ESTATUS' " & _
                                             "ELSE CFS.strContFisStatusIdentifier " & _
                                        "END) AS status, " & _
                                          "LINE.strShippingLineIdentifier, " & _
                                          "CATE.strContainerCatIdentifier , SQ.intServiceOrderId, SQ.intServiceQueuId " & _
                                          ", I.strContainerInvYardPositionId " & _
                                          ", I.strContainerInvYardPositionId " & _
                                          ", I.strContainerInvYardPositionId " & _
                                    "FROM tblclsServiceQueu SQ " & _
                                         "LEFT JOIN tblclsContainerInventory I " & _
                                           "ON SQ.intContainerUniversalId = I.intContainerUniversalId " & _
                                           "LEFT JOIN tblclsContainerFiscalStatus CFS " & _
                                             "ON I.intContFisStatusId =CFS.intContFisStatusId " & _
                                           "LEFT JOIN tblclsContainerCategory CATE " & _
                                              "ON I.intContainerCategoryId = CATE.intContainerCategoryId " & _
                                           "LEFT JOIN  tblclsShippingLine LINE " & _
                                             "ON I.intContainerInvOperatorId = LINE.intShippingLineId " & _
                                             "LEFT JOIN tblclsContainer CONT " & _
                                               "ON I.strContainerId = CONT.strContainerId " & _
                                               "LEFT JOIN tblclsContainerISOCode ISO " & _
                                                 "ON CONT.intContISOCodeId = ISO.intContISOCodeId " & _
                                                 "LEFT JOIN tblclsContainerSize S " & _
                                                   "ON ISO.intContainerSizeId = S.intContainerSizeId " & _
                                                   "LEFT JOIN tblclsContainerType T " & _
                                                     "ON ISO.intContainerTypeId = T.intContainerTypeId " & _
                                                     "LEFT JOIN tblclsService SV " & _
                                                       "ON SQ.intServiceId = SV.intServiceId " & _
                                "WHERE  SQ.blnServiceQueuExecuted = 0  AND " & _
                                             "SQ.dtmServiceQueuCheckIn IS NOT NULL AND " & _
                                          "SQ.dtmServiceQueuCheckOut IS NULL AND    " & _
                                          "SQ. dtmServiceQueuExecDate IS NULL AND " & _
                                          "SQ.intServiceId IN(SELECT SERV.intServiceId " & _
                                           "FROM tblclsService SERV " & _
                                          "WHERE SERV.strServiceIdentifier IN ('ENTLL','ENTV') ) " & _
                                          " AND ISNULL(I.intContainerUniversalId,0) > 0 " & _
                                          " AND  SQ.intVisitId=" & Convert.ToString(VisitId)

        iolecmd_comand.CommandText = ls_SQL_Command

        iAdapt_comand.SelectCommand = iolecmd_comand
        Try
            iAdapt_comand.SelectCommand.Connection.Open()
            iAdapt_comand.Fill(tablaQUERY)
        Catch ex As Exception
            Dim strError As String
            strError = ObtenerError(ex.Message, 99999)
        Finally
            iAdapt_comand.SelectCommand.Connection.Close()
            iAdapt_comand.SelectCommand.Connection.Dispose()

        End Try
        For Each valor As DataRow In tablaQUERY.Rows
            Dim registro As DataRow = tablafuente.NewRow()
            registro("cargado") = valor("cargado")
            registro("contenedor") = valor("strContainerId")
            registro("tipo") = valor("strContainerTypeIdentifier")
            registro("tamaño") = valor("strContainerSizeIdentifier")
            registro("lleno") = valor("blnContainerIsFull")
            registro("linea") = valor("strShippingLineIdentifier")
            registro("clase") = valor("strContainerCatIdentifier")
            registro("posicion") = valor("strContainerInvYardPositionId")
            registro("fecha") = Format(Date.Now, "dd/MM/yyyy HH:mm ")
            registro("iduniversal") = valor("intContainerUniversalId")
            registro("servicio") = valor("strServiceIdentifier")
            registro("status") = valor("status")
            registro("soid") = valor("intServiceOrderId")
            registro("queueid") = valor("intServiceQueuId")
            tablafuente.Rows.Add(registro)
        Next
        iAdapt_comand = Nothing

        Return tablafuente
    End Function

    '''''''''''''
    <WebMethod()> _
      Public Function Obtener_IdNumClase(ByVal Categoria As String) As Integer
        ' Dim Categoria As String = "AR"
        Dim myConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        Dim myConnection As New OleDbConnection(myConnectionString)
        Dim mySelectQuery = "SELECT intContainerCategoryId FROM tblclsContainerCategory	where strContainerCatIdentifier='" & Categoria & "'"
        Dim myCommand As New OleDbCommand(mySelectQuery, myConnection)

        '---------------------------
        Dim iadpt_ole As OleDbDataAdapter = New OleDbDataAdapter()
        Dim idat_Table As DataTable = New DataTable()
        iadpt_ole.SelectCommand = myCommand
        Dim iint_id As Integer = 0

        Try
            myConnection.Open()
            iadpt_ole.Fill(idat_Table)

            'revisar la informacion que trae la tabla 
            If idat_Table.Columns.Count = 1 And idat_Table.Rows.Count = 1 Then
                iint_id = Convert.ToInt64(idat_Table(0)(0))
            Else
                iint_id = 0
            End If


        Catch ex As Exception
            Dim lstr_Value As String
            lstr_Value = ex.Message
            iint_id = 0
        Finally
            iadpt_ole.SelectCommand.Connection.Close()
            myConnection.Close()

            iadpt_ole.Dispose()
            myConnection.Close()

        End Try

        iadpt_ole = Nothing
        myConnection = Nothing

        Return iint_id

    End Function
    '''''''''''''
    <WebMethod()> _
      Public Function Obtener_IdStringClase(ByVal Categoria As String) As String
        ' Dim Categoria As String = "AR"
        Dim myConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        Dim myConnection As New OleDbConnection(myConnectionString)
        Dim mySelectQuery = "SELECT strContainerCatIdentifier FROM tblclsContainerCategory	where intContainerCategoryId=" & Categoria & ""
        Dim myCommand As New OleDbCommand(mySelectQuery, myConnection)

        '---------------------------
        Dim iadpt_ole As OleDbDataAdapter = New OleDbDataAdapter()
        Dim idat_Table As DataTable = New DataTable()
        iadpt_ole.SelectCommand = myCommand
        Dim lstr_id As String = ""

        Try
            myConnection.Open()
            iadpt_ole.Fill(idat_Table)

            'revisar la informacion que trae la tabla 
            If idat_Table.Columns.Count = 1 And idat_Table.Rows.Count = 1 Then
                lstr_id = Convert.ToString(idat_Table(0)(0))
            Else
                lstr_id = ""
            End If


        Catch ex As Exception
            Dim lstr_Value As String
            lstr_Value = ex.Message
            lstr_id = ""
        Finally
            iadpt_ole.SelectCommand.Connection.Close()
            myConnection.Close()

            iadpt_ole.Dispose()
            myConnection.Close()

        End Try

        iadpt_ole = Nothing
        myConnection = Nothing

        Return lstr_id

    End Function

    '''' '''''''''''
    ''' '''''''''''''
    <WebMethod()> _
      Public Function Actualizar_ClaseInventario(ByVal txt_Comentarios As String, ByVal Clase_Seleccionada As String, ByVal aint_clasenumerica As Integer, ByVal id_Universal As Integer) As Integer

        'Dim txt_Comentarios As String = "DEAD NOTHE"
        'Dim Clase_Seleccionada As String = "AR"
        'Dim id_Universal As Integer = 8069992

        '  Dim resultado = Obtener_IdNumClase(Clase_Seleccionada).ToString()

        'Dim myReader As OleDbDataReader
        Dim myConnectionString = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        Dim myConnection As New OleDbConnection(myConnectionString)
        Dim mySelectQuery As String

        Try
            mySelectQuery = "update tblclsContainerInventory " & _
                 "set tblclsContainerInventory.strContainerInvComments = tblclsContainerInventory.strContainerInvComments + '" & txt_Comentarios & "', " & _
                 "tblclsContainerInventory.intContainerCategoryId = " & aint_clasenumerica.ToString() & _
                 "where tblclsContainerInventory.intContainerUniversalId = " & id_Universal
            Dim myCommand As New OleDbCommand(mySelectQuery, myConnection)
            myConnection.Open()
            'myReader = myCommand.ExecuteReader()
            myCommand.ExecuteNonQuery()

        Catch ex As Exception
            Return 0
            'myReader.Close()
            Exit Function
        Finally
            myConnection.Close()
            myConnection.Dispose()
        End Try
        myConnection = Nothing
        Return 1
        'myReader.Close()
    End Function

    '''''''actualiza historico y manda llamar a actualizar clase inventario'''''''''''''''
    <WebMethod()> _
     Public Function Actualizar_clase_Contenedor_Main(ByVal UniversalId As Integer, ByVal aint_categoryid As Integer, ByVal Comments As String, ByVal User As String) As Integer
        '-----------------------------
        'Dim UniversalId As Integer = 806999
        'Dim CategId As Integer = 46
        'Dim Comments As String = "Dead Nothe"
        'Dim User As String = "RSathielle"

        Dim oleDBconnx As OleDbConnection
        Dim oleDBcom As OleDbCommand
        oleDBcom = New OleDbCommand()
        oleDBconnx = New OleDbConnection()
        Dim strconx As String
        Dim lstr_categoryID As String

        strconx = ConfigurationManager.ConnectionStrings("dbCalathus").ConnectionString
        oleDBconnx.ConnectionString = strconx
        oleDBcom = oleDBconnx.CreateCommand
        '----------------------------------



        Dim oleDb_param As OleDbParameter = New OleDbParameter()
        Dim ls_sql As String
        Dim ol_param_Universal As OleDbParameter = New OleDbParameter()
        Dim ol_param_CategId As OleDbParameter = New OleDbParameter()
        Dim ol_param_Comments As OleDbParameter = New OleDbParameter()
        Dim ol_param_User As OleDbParameter = New OleDbParameter()

        Dim oleDb_paramOut As OleDbParameter = New OleDbParameter()


        ' obtener el id de categoria 
        lstr_categoryID = Obtener_IdStringClase(aint_categoryid)

        ' ver si hay categoria con texto
        If lstr_categoryID.Length = 0 Then
            'retornar error 
            Return 0
        End If

        '' ''' '''''''
        ol_param_Universal.ParameterName = "@UniversalId"
        ol_param_Universal.OleDbType = OleDbType.Integer
        ol_param_Universal.Value = UniversalId

        ol_param_CategId.ParameterName = "@CategId"
        ol_param_CategId.OleDbType = OleDbType.Integer
        ol_param_CategId.Value = aint_categoryid

        'agregando fraccionarios
        ol_param_Comments.ParameterName = "@Comments"
        ol_param_Comments.OleDbType = OleDbType.Char
        ol_param_Comments.Value = Comments

        ol_param_User.ParameterName = "@User"
        ol_param_User.OleDbType = OleDbType.Char
        ol_param_User.Value = User

        'oleDb_paramOut.ParameterName = "@intErrorCode"
        'oleDb_paramOut.OleDbType = OleDbType.Integer
        'oleDb_paramOut.Direction = ParameterDirection.Output

        ls_sql = "spUpdateHistoryCategory"

        oleDBcom.CommandText = ls_sql
        oleDBcom.CommandType = CommandType.StoredProcedure
        oleDBcom.Parameters.Add(ol_param_Universal) '(intcontaineruniversalid) 'Id universal del contenedor)
        oleDBcom.Parameters.Add(ol_param_CategId)
        oleDBcom.Parameters.Add(ol_param_Comments)
        oleDBcom.Parameters.Add(ol_param_User)
        oleDBcom.CommandTimeout = 0
        Try
            oleDBconnx.Open()
            oleDBcom.ExecuteNonQuery()

        Catch ex As Exception
            Return 0
            Exit Function
        Finally
            oleDBconnx.Close()
            oleDBcom.Connection.Close()
            oleDBcom.Connection.Dispose()
            oleDBconnx.Dispose()

        End Try
        oleDBconnx = Nothing
        oleDBcom = Nothing
        '' llamar a actualizar clase 
        Dim lint_result As Integer

        lint_result = Actualizar_ClaseInventario(Comments, lstr_categoryID, aint_categoryid, UniversalId)
        ' es 0 hay error 
        If lint_result = 0 Then
            Return 0
        End If

        Return 1
    End Function
    ''''''''''''''''''''''''
    '''''''''''''''''''''''''
    ''''''''''''''
    '''''''''''''''
    '''''''''''

    ''''''''''''''''''''''''''

    'Esta funcion retorna una tabla con un mensaje de error
    Public Function dt_RetrieveErrorTable(ByVal astr_Message As String) As DataTable

        Dim ldt_ErrorTable As DataTable
        Dim lrw_Error As DataRow

        ldt_ErrorTable = New DataTable("ErrorTable")
        ldt_ErrorTable.Columns.Add("Error", GetType(String))
        lrw_Error = ldt_ErrorTable.NewRow()

        lrw_Error("Error") = astr_Message
        ldt_ErrorTable.Rows.Add(lrw_Error)
        Return ldt_ErrorTable

    End Function


    Public Function of_getTimeoutSearch() As Long

        Dim llong_value As Long = 0
        Dim lstr_value As String

        Try
            lstr_value = ConfigurationManager.AppSettings.Item("SearchTimeOut").ToString()

            If Long.TryParse(lstr_value, llong_value) = False Then
                llong_value = 0
            End If
        Catch ex As Exception
            llong_value = 0
        End Try

        Return llong_value

    End Function

    Public Function of_getSearchTries() As Integer

        Dim llong_value As Long = 0
        Dim lstr_value As String

        Try
            lstr_value = ConfigurationManager.AppSettings.Item("SearchTrys").ToString()

            If Long.TryParse(lstr_value, llong_value) = False Then
                llong_value = 0
            End If
        Catch ex As Exception
            llong_value = 0
        End Try

        Return llong_value


    End Function



    Public Sub CopyTable(ByVal atb_Original As DataTable, ByRef atb_Destiny As DataTable)

        atb_Destiny = New DataTable()
        Dim lcolum_new As DataColumn = New DataColumn()
        Dim lrow_new As DataRow

        For Each lcolum_table As DataColumn In atb_Original.Columns
            lcolum_new = New DataColumn(lcolum_table.ColumnName)
            lcolum_new.DataType = lcolum_table.DataType
            lcolum_new.Caption = lcolum_table.Caption
            atb_Destiny.Columns.Add(lcolum_new)
        Next

        For Each lrow_original As DataRow In atb_Original.Rows

            lrow_new = atb_Destiny.NewRow()

            For lint_index = 0 To atb_Original.Columns.Count - 1
                Try
                    lrow_new(lint_index) = HttpUtility.HtmlDecode(lrow_original(lint_index))
                Catch ex As Exception

                End Try

            Next

            atb_Destiny.Rows.Add(lrow_new)

        Next

    End Sub
End Class