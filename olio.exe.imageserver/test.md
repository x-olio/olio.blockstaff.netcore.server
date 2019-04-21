help[方法：jsonrpc 功能：帮助]
  method = help
  params = []
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=help&params=[]

usernew[方法:jsonrpc 功能:用户注册]
  method = user_new
  params = [userid(string),passhash(hexstr)]
0.error param
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_new&params=[]
1.user_new  abc,00
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_new&params=[%22abc%22,%2200%22]

userlogin[方法:jsonrpc 功能:用户登录]
  method = user_login
  params = [userid(string),passhash(hexstr)]
0.error param
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_login&params=[]