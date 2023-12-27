export DEBIAN_FRONTEND=noninteractive

apt-get update && \
	apt-get install -qy openssh-server g++ binutils \
			   libgtest-dev \
			   gdb rsync zip
