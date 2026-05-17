import chromadb
import os

# 设置数据存储路径
data_path = os.path.join(os.path.dirname(__file__), "chroma_data")
os.makedirs(data_path, exist_ok=True)

print(f"启动 ChromaDB，数据存储路径: {data_path}")

# 创建持久化客户端
client = chromadb.PersistentClient(path=data_path)

# 创建一个测试集合
collection = client.get_or_create_collection("sales_data")

print("ChromaDB 服务已启动，数据存储在: ", data_path)
print("服务地址: http://localhost:8000")
print("API版本: v1")

# 保持服务运行
import time
while True:
    time.sleep(1)