#include <stdio.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/process.h>
#include <arpa/inet.h>

#define ssend(socket, str) send(socket, str, strlen(str), 0)

#define BUF_LEN		2048
#define MAX_LEN		2047

#define SUCCESS 0
#define FAILED -1


static int connect_to_webman()
{
	struct sockaddr_in sin;
	int s;

	sin.sin_family = AF_INET;
	sin.sin_addr.s_addr = 0x7F000001;	//127.0.0.1 (localhost)
	sin.sin_port = htons(80);			//http port (80)
	s = socket(AF_INET, SOCK_STREAM, 0);
	if(s < 0)
	{
		return -1;
	}

	if(connect(s, (struct sockaddr *)&sin, sizeof(sin)) < 0)
	{
		return -1;
	}

	return s;
}

static char h2a(char hex)
{
	char c = hex;
	if(c>=0 && c<=9)
		c += '0';
	else if(c>=10 && c<=15)
		c += 0x57; //a-f
	return c;
}

static void urlenc(char *dst, char *src)
{
	size_t j = 0;
	size_t n = strlen(src);
	for(size_t i = 0; i < n; i++, j++)
	{
		if(j >= MAX_LEN) {j = MAX_LEN; break;}

			 if(src[i]==' ') {dst[j++] = '%'; dst[j++] = '2'; dst[j] = '0';}
		else if(src[i] & 0x80)
		{
			dst[j++] = '%';
			dst[j++] = h2a((unsigned char)src[i]>>4);
			dst[j]   = h2a(src[i] & 0xf);
		}
		else if(src[i] == '"') {dst[j++] = '%'; dst[j++] = '2'; dst[j] = '2';}
		else if(src[i] == '#') {dst[j++] = '%'; dst[j++] = '2'; dst[j] = '3';}
		else if(src[i] == '%') {dst[j++] = '%'; dst[j++] = '2'; dst[j] = '5';}
		else if(src[i] == '&') {dst[j++] = '%'; dst[j++] = '2'; dst[j] = '6';}
		else if(src[i] == '\''){dst[j++] = '%'; dst[j++] = '2'; dst[j] = '7';}
		else if(src[i] == '+') {dst[j++] = '%'; dst[j++] = '2'; dst[j] = 'B';}
		else if(src[i] == '?') {dst[j++] = '%'; dst[j++] = '3'; dst[j] = 'F';}
		else dst[j] = src[i];
	}
	dst[j] = '\0';

	sprintf(src, "%s", dst);
}

static void log_local_file(const char *title_id, const char *name, const char *text)
{
	return;
	char path[200];
	sprintf(path, "/dev_hdd0/game/%s/USRDIR/%s", title_id, name);
	FILE *fp = fopen(path,	"wb");
	fwrite((void *) text, 1, strlen(text), fp);
	fclose(fp);
}

static int syscall_sfo(char* sfo)
{
	lv2syscall1(30, (uint64_t)sfo);
	return_to_user_prog(int);
}

// 10-byte zero-terminated string like "PKGLAUNCH\0"
static void get_title_id(char* title_id)
{
	char sfo[0x40];
	syscall_sfo(sfo);
	memcpy(title_id, &sfo[1], 10);
}

int main(int argc, const char* argv[])
{
	char path[BUF_LEN], url[BUF_LEN], param[BUF_LEN], launchtxt[BUF_LEN];
	char *p;
	memset(path,  0, BUF_LEN);
	memset(param, 0, BUF_LEN);
	memset(url,   0, BUF_LEN);
	memset(launchtxt, 0,  BUF_LEN);

	char title_id[10];
	get_title_id(title_id);
	log_local_file(title_id, "0", "a");
	sprintf(launchtxt, "/dev_hdd0/game/%s/USRDIR/launch.txt", title_id);
	log_local_file(title_id, "1_launchtxt", launchtxt);
	
	FILE *fp;
	fp = fopen(launchtxt, "rb");
	if (fp)
	{
		fread((void *) path, 1, MAX_LEN, fp);
		fclose(fp);

		if(*path)
		{
			// copy path to param if it has \n, also nuke \n in param just in case
			p = strstr(path, "\n");
			if(p) {
				*p = 0;
				if(*param == 0) 
				{
					sprintf(param, "%s", p + 1);
				}
				p = strstr(param, "\n");
				if(p) 
				{
					*p = 0;
				}
			}

			// copy path to param if not already, until \r, also nuke \r in param just in case
			p = strstr(path, "\r");
			if(p) 
			{
				*p = 0;
				if(*param == 0)
				{
					sprintf(param, "%s", p + 1);
				}
			}
			p = strstr(param, "\r");
			if(p) 
			{
				*p = 0;
			}

			// format path into url
			// startswith "GET "
			if(!strncmp(path, "GET ", 4)) // startswith GET
				{urlenc(url, path); sprintf(url, "%s HTTP/1.0\r\n", path);}
			else
			// startswith http://127.0.0.1/ or http://localhost/ => copy without trailing /
			if(!strncmp(path, "http://127.0.0.1/", 17) || !strncmp(path, "http://localhost/", 17))
				{urlenc(url, path); sprintf(url, "GET %s HTTP/1.0\r\n", path + 16);}
			else
			// startswith / and contains .ps3 => format into http request
			if((*path == '/') && (strstr(path, ".ps3") != NULL))
				{urlenc(url, path); sprintf(url, "GET %s HTTP/1.0\r\n", path);}
			else
			// startswith / and contains _ps3 => format into http request
			if((*path == '/') && (strstr(path, "_ps3") != NULL))
				{urlenc(url, path); sprintf(url, "GET %s HTTP/1.0\r\n", path);}
			else
			// startswith /mount.ps2/ => format into http request
			if(!strncmp(path, "/mount.ps2/", 11))
				{urlenc(url, path); sprintf(url, "GET %s HTTP/1.0\r\n", path);}
			else
			// startswith /mount.ps2/ => format into http request
			if(!strncmp(path, "/mount_ps2/", 11))
				{urlenc(url, path); sprintf(url, "GET %s HTTP/1.0\r\n", path);}
			else
			// ( startswith /dev_ or /net ) and ( contains /GAME or /*ISO or / or .ntfs[ ) => GET mount_ps3/dev_*/*ISO/...
			if((!strncmp(path, "/dev_", 5) || !strncmp(path, "/net", 4)) && ((strstr(path, "/GAME") != NULL) || (strstr(path, "/PS3ISO") != NULL) || (strstr(path, "/PSXISO") != NULL) || (strstr(path, "/PS2ISO") != NULL) || (strstr(path, "/PSPISO") != NULL) || (strstr(path, "/DVDISO") != NULL) || (strstr(path, "/BDISO") != NULL) || (strstr(path, ".ntfs[") != NULL)))
				{urlenc(url, path); sprintf(url, "GET /mount_ps3%s HTTP/1.0\r\n", path);}
		}
	}
	else
		{
			log_local_file(title_id, "5_end_notxt", "a");
			return 0; // launch.txt was not found
		}

	log_local_file(title_id, "2_path", path);
	log_local_file(title_id, "3_param", param);
	log_local_file(title_id, "4_url", url);

	///////////////////////
	// process path + param
	///////////////////////
	if(strstr(path, "/EBOOT.BIN") || strstr(path, ".self") || strstr(path, ".SELF")) goto exec;


	//////////////
	// process URL
	//////////////
	if(*url)
	{
		int s = connect_to_webman();
		if(s >= 0) ssend(s, url);
		log_local_file(title_id, "5_end_http", "a");
		return 0;
	}

	log_local_file(title_id, "5_end_wtf", "a");

exec:

	if(*param)
	{
		char* launchargv[2];
		memset(launchargv, 0, sizeof(launchargv));

		// nuke \r and \n in param, with prejudice this time
		p = strstr(param, "\n"); if(p) *p = 0;
		p = strstr(param, "\r"); if(p) *p = 0;

		launchargv[0] = (char*)malloc(strlen(param) + 1); strcpy(launchargv[0], param);
		launchargv[1] = NULL;

		sysProcessExitSpawn2((const char*)path, (char const**)launchargv, NULL, NULL, 0, 3071, SYS_PROCESS_SPAWN_STACK_SIZE_1M);
	}
	else
		sysProcessExitSpawn2((const char*)path, NULL, NULL, NULL, 0, 1001, SYS_PROCESS_SPAWN_STACK_SIZE_1M);

	return 0;
}
