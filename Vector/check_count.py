import chromadb

client = chromadb.HttpClient(host='localhost', port=8000)
collection = client.get_collection(name='sales_data_collection')
count = collection.count()
print(f"数据数量: {count}")

results = collection.get()
print(f"所有ID: {results['ids']}")