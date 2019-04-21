# olio.imageserver

olio.imageserver 是为了保存用户数据而设计的服务器，当前功能比较简单。实现了基本的功能
## 要点
olio.imageserver 设计为可长期保存，易于复制，易于溯源

采用rocksdb作为存储库，并保存每一步的存储log

将来可轻易开发备份和复制功能

## 版本

当前版本已经在

http://cafe.f3322.net:17280/

部署

## 接口
接口分为http 和 jsonrpc两类

获取资源的接口因为 jsonrpc只能返回json，不适合，故不用jsonrpc

上传资源的接口因为 jsonrpc只能上传json，不适合，故不用。

### getuserasset

   方法 Http Get 或者 Post

   参数 user[string,必选，用户名]  

   参数 key[string,必选,资源名称]

   参数 format[string，可选，资源返回格式]

     格式 hexstr（返回hexstr） 
     格式 string(转换为string)
     格式 image（转换为image）
     格式 其它（二进制返回）
   
   例子

     http://cafe.f3322.net:17280/getuserasset?user=abc&key=hello&format=image

   如果找不到资源就会404

### getraw
   
   方法 Http Get 或者 Post

   参数 id[hexstring,必选，资源ID,资源ID就是资源的SHA256计算得出]
  
   例子

    http://cafe.f3322.net:17280/getraw?id=34B723222811955BF708B1D252E89ED67E587FBFC6A6BDAAA0E0BD5BA8CABE5E&format=image

   如果找不到资源就会404

### uploadraw

  方法 Http Post, 必须使用post方式
  
  参数 [user,string,必选，用户名]
  
  参数 [token,hexstring,必选，登陆返回的token]
  
  参数 [file,form upload file,必选，上传的文件]

  例子
    [post] http://cafe.f3322.net:17280/uploadraw 
   post user,token,file[0]


### help
 
  方法 JSONRPC, 功能：帮助
  
  params = []

  返回值  

   result.msg 消息
   
   result.height 数据高度

  例子
 
    http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=help&params=[]

### user_new
 
  方法 jsonrpc, 功能:用户注册
  
  params = [userid(string),passhash(hexstr)]
  
  返回值
   
   result.result 注册是否成功

  例子

   http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=user_new&params=["abc","00"]

### user_login
  
  方法:jsonrpc 功能:用户登录]
  
  method = user_login

  params = [userid(string),passhash(hexstr)]

  返回值

   result.result

   result.token

  例子

   http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=user_login&params=["abc","00"]

### user_setnamedasset

  方法:jsonrpc 功能：设置用户命名资源

  method=user_setnamedasset
  
  params=[userid(string),token(hexstr),key(string),data(hexstr)]
  
  返回值
  
   result.result

  例子

   http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=user_setnamedasset&params=["abc","00","hello","010203"]

### user_listnamedasset

  方法:jsonrpc 功能：列表用户所设置的有名字的资源]

  method=user_listnamedasset

  params=[userid(string),token(hexstr)]

  返回值

   result.result

   result.list

  例子

   http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=user_listnamedasset&params=["abc","00"]
