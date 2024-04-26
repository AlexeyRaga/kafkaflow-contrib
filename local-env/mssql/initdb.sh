#!/usr/bin/env bash

# Start SQL Server
/opt/mssql/bin/sqlservr &

# Wait for SQL to start
TRIES=60
DBSTATUS=1
ERRCODE=1
i=0

echo "Waiting for SQL Server to start"
while [[ $DBSTATUS -ne 0 ]] && [[ $i -lt $TRIES ]]; do
	i=$((i+1))
	DBSTATUS=$(/opt/mssql-tools/bin/sqlcmd -h -1 -t 1 -U sa -P $MSSQL_SA_PASSWORD -Q "SET NOCOUNT ON; Select COALESCE(SUM(state), 0) from sys.databases") || DBSTATUS=1
	if [ $DBSTATUS -ne 0 ]; then
        sleep 1s
	fi
done

sleep 5s
if [ $DBSTATUS -ne 0 ]; then
	echo "SQL Server took more than $TRIES seconds to start up or one or more databases are not in an ONLINE state"
	exit 1
fi

mssql=( /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d master -i )

find /docker-entrypoint-initdb.d -mindepth 2 -type f | sort | while read f; do
  case "$f" in
    *.sh)
      if [ -x "$f" ]; then
        echo "$0: running $f"
        "$f"
      else
        echo "$0: sourcing $f"
        . "$f"
      fi
      ;;
    *.sql)    echo "$0: running $f"; "${mssql[@]}" "$f"; echo ;;
    *.sql.gz) echo "$0: running $f"; gunzip -c "$f" | "${mssql[@]}"; echo ;;
    *)        echo "$0: ignoring $f" ;;
  esac
  echo
done

touch /tmp/mssql.ready
echo "SQL Server is running"
sleep infinity
