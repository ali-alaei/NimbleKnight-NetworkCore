
FOR /L %%A IN (1,1,10) DO (
  timeout /t 10 /nobreak
  start test.exe
)

