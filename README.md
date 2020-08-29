-In the file dump.sqlpwd are the settings needed for the connection with mysql
-To install the program as a service do use this in cmd :
	sc create backupservice binPath="absolute route of the .exe" obj= "absolute name of the user you want to execute the service" Password="password of that user"

-To modify the files you want to backup go to appsettings.json, if the service is running they will aply the next iteration 
	·The format of DayOfTheWeek in Time must be numbers between 0 to 6 being 0 sunday and 1 monday, to do backups all days of the week use *
	·ExcludedDatabases must be the name of the databases in between '' and separated with comas 
	·IncludedDatabases can be * to include all and them exclude the ones you dont want, if not put the name of the databases separated with |
	·The names of the directorys must be tha absolute path using double \\ becuse \ is a scape caracter
	·To exclude extensions of files to not copy them you must put the name of the extensions with \\ at the end and separate them withspace , example: .pdf\\ .txt\\ .csv\\
	.To include extensions is the same format to exclude them but you can use * to include all
	·To select the compress extension you can put any extension that your computer can work with

-To copy to a remote computer using WinSCP since is more advanced and it depends a lot of the settings of your connection is no included in the appsetting.json,
to do that you need to go to App.cs and put your settings in CopyWithWinSCP
