http方法
getuserasset
  参数[user,key,format]
   格式 hexstr（返回hexstr） ，string(转换为string)，image（转换为image），其它（二进制返回）
http://127.0.0.1/getuserasset?user=abc&key=hello&format=image&r=1112

rpc方法

help[方法：jsonrpc 功能：帮助]
  method = help
  params = []
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=help&params=[]

usernew[方法:jsonrpc 功能:用户注册]
  method = user_new
  params = [userid(string),passhash(hexstr)]
  返回值
  bool result.result
0.error param
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_new&params=[]
1.user_new  abc,00
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_new&params=["abc","00"]

userlogin[方法:jsonrpc 功能:用户登录]
  method = user_login
  params = [userid(string),passhash(hexstr)]
  返回值
  bool result.result
  hexstr token [成功才有]
0.error param
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_login&params=[]
1.user_login abc,00
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_login&params=["abc","00"]

user_setnamedasset[方法:jsonrpc 功能：设置用户命名资源]
  method=user_setnamedasset
  params=[userid(string),token(hexstr),key(string),data(hexstr)]
  返回值
  bool result.result
0.error param
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_setnamedasset&params=[]

00要替换成登陆所得的token
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_setnamedasset&params=["abc","00","hello","010203"]

user_listnamedasset[方法:jsonrpc 功能：列表用户所设置的有名字的资源]
  method=user_listnamedasset
  params=[userid(string),token(hexstr)]
  返回值
  bool result.result
  string result.list

0.error param
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_listnamedasset&params=[]

00要替换成登陆所得的token
http://127.0.0.1/rpc?jsonrpc=2.0&id=1&method=user_listnamedasset&params=["abc","00"]
