# olio.imageserver

olio.imageserver ��Ϊ�˱����û����ݶ���Ƶķ���������ǰ���ܱȽϼ򵥡�ʵ���˻����Ĺ���
## Ҫ��
olio.imageserver ���Ϊ�ɳ��ڱ��棬���ڸ��ƣ�������Դ

����rocksdb��Ϊ�洢�⣬������ÿһ���Ĵ洢log

���������׿������ݺ͸��ƹ���

## �汾

��ǰ�汾�Ѿ���

http://cafe.f3322.net:17280/

����

## �ӿ�
�ӿڷ�Ϊhttp �� jsonrpc����

��ȡ��Դ�Ľӿ���Ϊ jsonrpcֻ�ܷ���json�����ʺϣ��ʲ���jsonrpc

�ϴ���Դ�Ľӿ���Ϊ jsonrpcֻ���ϴ�json�����ʺϣ��ʲ��á�

### getuserasset

   ���� Http Get ���� Post

   ���� user[string,��ѡ���û���]  

   ���� key[string,��ѡ,��Դ����]

   ���� format[string����ѡ����Դ���ظ�ʽ]

     ��ʽ hexstr������hexstr�� 
     ��ʽ string(ת��Ϊstring)
     ��ʽ image��ת��Ϊimage��
     ��ʽ �����������Ʒ��أ�
   
   ����

     http://cafe.f3322.net:17280/getuserasset?user=abc&key=hello&format=image

   ����Ҳ�����Դ�ͻ�404

### getraw
   
   ���� Http Get ���� Post

   ���� id[hexstring,��ѡ����ԴID,��ԴID������Դ��SHA256����ó�]
  
   ����

    http://cafe.f3322.net:17280/getraw?id=34B723222811955BF708B1D252E89ED67E587FBFC6A6BDAAA0E0BD5BA8CABE5E&format=image

   ����Ҳ�����Դ�ͻ�404

### uploadraw

  ���� Http Post, ����ʹ��post��ʽ
  
  ���� [user,string,��ѡ���û���]
  
  ���� [token,hexstring,��ѡ����½���ص�token]
  
  ���� [file,form upload file,��ѡ���ϴ����ļ�]

  ����
    [post] http://cafe.f3322.net:17280/uploadraw 
   post user,token,file[0]


### help
 
  ���� JSONRPC, ���ܣ�����
  
  params = []

  ����ֵ  

   result.msg ��Ϣ
   
   result.height ���ݸ߶�

  ����
 
    http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=help&params=[]

### user_new
 
  ���� jsonrpc, ����:�û�ע��
  
  params = [userid(string),passhash(hexstr)]
  
  ����ֵ
   
   result.result ע���Ƿ�ɹ�

  ����

   http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=user_new&params=["abc","00"]

### user_login
  
  ����:jsonrpc ����:�û���¼]
  
  method = user_login

  params = [userid(string),passhash(hexstr)]

  ����ֵ

   result.result

   result.token

  ����

   http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=user_login&params=["abc","00"]

### user_setnamedasset

  ����:jsonrpc ���ܣ������û�������Դ

  method=user_setnamedasset
  
  params=[userid(string),token(hexstr),key(string),data(hexstr)]
  
  ����ֵ
  
   result.result

  ����

   http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=user_setnamedasset&params=["abc","00","hello","010203"]

### user_listnamedasset

  ����:jsonrpc ���ܣ��б��û������õ������ֵ���Դ]

  method=user_listnamedasset

  params=[userid(string),token(hexstr)]

  ����ֵ

   result.result

   result.list

  ����

   http://cafe.f3322.net:17280/rpc?jsonrpc=2.0&id=1&method=user_listnamedasset&params=["abc","00"]
