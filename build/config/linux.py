import os.path
from waflib import Utils
from waflib.TaskGen import feature

archs = [ 'x86', 'x86_64' ]

tools = [
	'gcc',
	'gxx',
	'cs',
	'resx',
	'utils',
	'externals',
	'test',
	'version',
	'xcompile',
]

def prepare(conf):
	root = conf.path.abspath()
	env = conf.env
	j = os.path.join

	env['MCS']  = 'dmcs'
	env['CC']   = 'gcc-4.6'
	env['CXX']  = 'g++-4.6'

	env['ARCH']    = ['-m%s' % ('64' in env.SUBARCH and '64' or '32')]
	env['ARCH_ST'] = env['ARCH']

	pin = j(root, '3rdParty', 'pin-2.11-49306-gcc.3.4.6-ia32_intel64-linux')

	env['EXTERNALS_x86'] = {
		'pin' : {
			'INCLUDES'  : [
				j(pin, 'source', 'include'),
				j(pin, 'source', 'include', 'gen'),
				j(pin, 'extras', 'components', 'include'),
				j(pin, 'extras', 'xed2-ia32', 'include'),
			],
			'STLIBPATH'   : [
				j(pin, 'ia32', 'lib'),
				j(pin, 'ia32', 'lib-ext'),
				j(pin, 'extras', 'xed2-ia32', 'lib'),
			],
			'STLIB'     : [ 'dwarf', 'elf', 'pin', 'xed' ],
			'DEFINES'   : [ 'BIGARRAY_MULTIPLIER=1', 'TARGET_LINUX', 'TARGET_IA32', 'HOST_IA32', 'USING_XED', ],
			'CFLAGS'    : [],
			'CXXFLAGS'  : [],
			'LINKFLAGS' : [],
		},
	}

	env['EXTERNALS_x86_64'] = {
		'pin' : {
			'INCLUDES'  : [
				j(pin, 'source', 'include'),
				j(pin, 'source', 'include', 'gen'),
				j(pin, 'extras', 'components', 'include'),
				j(pin, 'extras', 'xed2-intel64', 'include'),
			],
			'STLIBPATH'   : [
				j(pin, 'intel64', 'lib'),
				j(pin, 'intel64', 'lib-ext'),
				j(pin, 'extras', 'xed2-intel64', 'lib'),
			],
			'STLIB'     : [ 'dwarf', 'elf', 'xed' ],
			'DEFINES'   : [ 'BIGARRAY_MULTIPLIER=1', 'TARGET_LINUX', 'TARGET_IA32E', 'HOST_IA32E', 'USING_XED', ],
			'CFLAGS'    : [],
			'CXXFLAGS'  : [],
			'LINKFLAGS' : [],
		},
	}

	env['EXTERNALS'] = env['EXTERNALS_%s' % env.SUBARCH]

	env.append_value('supported_features', [
		'linux',
		'c',
		'cstlib',
		'cshlib',
		'cprogram',
		'cxx',
		'cxxstlib',
		'cxxshlib',
		'cxxprogram',
		'fake_lib',
		'cs',
		'test',
	])

def configure(conf):
	env = conf.env

	env.append_value('CSFLAGS', [
		'/warn:4',
		'/define:PEACH',
	])

	env.append_value('CSFLAGS_debug', [
		'/define:DEBUG,TRACE',
	])
	
	env.append_value('CSFLAGS_release', [
		'/define:TRACE',
		'/optimize+',
	])

	env['CSPLATFORM'] = 'anycpu'

	env.append_value('DEFINES_debug', [
		'DEBUG',
	])

	cppflags = [
		'-pipe',
		'-Werror',
		'-Wno-unused',
	]
	
	cppflags_debug = [
		'-ggdb',
	]

	cppflags_release = [
		'-O3',
	]

	env.append_value('CPPFLAGS', cppflags)
	env.append_value('CPPFLAGS_debug', cppflags_debug)
	env.append_value('CPPFLAGS_release', cppflags_release)

	env.append_value('LIB', [ 'dl' ])

	env['VARIANTS'] = [ 'debug', 'release' ]

def debug(env):
	env.CSDEBUG = 'full'

def release(env):
	env.CSDEBUG = 'pdbonly'
