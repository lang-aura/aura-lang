import os
import shutil

if __name__ == '__main__':
	path = "./AuraLang.Test/Integration/Examples/build/pkg"
	for name in os.listdir(path):
		if os.path.isdir(os.path.join(path, name)):
			if name == "stdlib":
				continue
			shutil.rmtree(os.path.join(path, name))
		else:
			if name == "go.mod":
				continue
			os.remove(os.path.join(path, name))