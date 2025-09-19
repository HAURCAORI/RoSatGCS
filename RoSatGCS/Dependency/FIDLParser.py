from enum import Enum
from typing import Callable, Dict, List, TYPE_CHECKING
from collections import OrderedDict
import sys, getopt
import os.path
import re
import json

# Global Variable
place_holder_none = 0;  place_holder_0 = 1;  place_holder_1 = 2;  place_holder_2 = 3;  place_holder_3 = 4

## match [global func]
#  @description         : matching the all regex
#  @param regex         : regex
#  @param text          : string to match
#  @param place_holder  : return the specific value using 'place_holder_x'
def match(regex, text: str, place_holder: int = place_holder_0):
    ret = re.fullmatch(regex, text)
    if ret:
        return [ret, ret.group(place_holder).strip()]
    return [ret, text]

## FIDLParser [class]
#  @description         : class of FIDLParser
class FIDLParser:
#region FIDLParser Types
    ## Scope [enum type]
    #  @description     : Parsing scope which specified by brackets
    class Scope(Enum):
        Root        =  0;   TypeCollection  =  1;   Interface       =  2;   Version     =  3
        Enumeration =  4;   Struct          =  5;   Method          =  6;   MethodIn    =  7
        MethodOut   =  8;   MethodError     =  9;   Union           = 10;   Map         = 11
        Broadcast   = 12;   BroadcastOut    = 13;   PSM             = 14;   PSMState    = 15

    ## Type [enum type]
    #  @description     : Available parsing type. Array type is specified by '[]' symbol
    class Type(Enum):
        # Basic Type
        UInt8       =  1;   Int8            =  2;   UInt16          =  3;   Int16       =  4
        UInt32      =  5;   Int32           =  6;   UInt64          =  7;   Int64       =  8
        Integer     =  9;   Boolean         = 10;   Float           = 11;   Double      = 12
        String      = 13;   ByteBuffer      = 14;   Enumeration     = 15;   Struct      = 16
        Method      = 17;   Union           = 18;   Map             = 19;   UserDefined = 20
        EnumElement = 21;   Null            =  0
        # Array Type
        AUInt8      = 31;   AInt8           = 32;   AUInt16         = 33;   AInt16      = 34
        AUInt32     = 35;   AInt32          = 36;   AUInt64         = 37;   AInt64      = 38
        AInteger    = 39;   ABoolean        = 40;   AFloat          = 41;   ADouble     = 42
        AString     = 43;   AByteBuffer     = 44;   AUserDefined    = 50

        _Offset     = 30

        def is_array(self) -> bool:
            return self.value > 30

        def to_array(self, convert:bool = True) -> 'FIDLParser.Type':
            if self.is_array() or not convert:
                return self
            return FIDLParser.Type(self.value + self._Offset.value)

        def base(self) -> 'FIDLParser.Type':
            if self.is_array():
                return FIDLParser.Type(self.value - self._Offset.value)
            return self

    ## MethodType [enum type]
    #  @description     : Method information
    class MethodType(Enum):
        Null = 0;  In = 1;  Out = 2

    ## ParseError [exception]
    #  @description     : Custom exception to handle
    class ParseError(Exception):
        def __init__(self, value):
            self.value = value

        def __str__(self):
            return self.value
#endregion

#region Parsing Context
    ## Context [class]
    #  @description: parsing results will be saved here
    class Context:
        ## Node [class type]
        #  @description: Node of the element
        class Node:
            def __init__(self, name_: str, desc_: str, id_: int, type_: 'FIDLParser.Type', info_: str = ""):
                self.name                   = name_
                self.description            = desc_
                self.id                     = id_
                self.type: FIDLParser.Type  = type_
                self.info                   = info_
                self.sub_nodes: List[FIDLParser.Context.Node] = []

        ## Method [class type]
        #  @description: Method of the element
        class Method:
            def __init__(self, name_: str, desc_: str, id_: int):
                self.name           = name_
                self.description    = desc_
                self.id             = id_
                self.input: List[FIDLParser.Context.Node]  = []
                self.output: List[FIDLParser.Context.Node] = []

        ## Interface [class type]
        #  @description: FIDL interface
        class Interface:
            def __init__(self, name_: str, desc_: str, id_: int):
                self.name = name_
                self.description = desc_
                self.id = id_
                self.major = -1
                self.minor = -1
                self.nodes: List[FIDLParser.Context.Node]     = []
                self.methods: List[FIDLParser.Context.Method] = []

            def find_type(self, s: str):
                ret = next((item for item in self.nodes if item.name == s), None)
                if ret:
                    return ret.type
                return FIDLParser.Type.Null

            def find_node(self, s: str):
                return next((item for item in self.nodes if item.name == s), None)

        ## __init__
        #  @description: Initialize the context
        def __init__(self):
            self.package            = ""
            self.interfaces:List[FIDLParser.Context.Interface] = []

    ## Tag [class]
    #  @description: comment of the FIDL file
    class Tag:
        def __init__(self):
            self.description    = ""
            self.details        = ""
            self.valid          = False

        def set_description(self, s: str):
            self.description    = s
            self.valid          = True

        def set_details(self, s: str):
            self.details        = s
            self.valid          = True

        def get(self):
            if not self.valid:
                return ['','']
            self.valid = False
            des = self.description.replace('\t', ' ').strip()
            det = self.details.replace('\t', ' ').strip()
            self.description = ""
            self.details = ""
            return [' '.join(des.split()), ' '.join(det.split())]
#endregion

#region Regex Definition
    ## match_regex [func]
    #  @description: return the regex which represents '[symbol] [names]' syntax
    #                place_holder_0 returns the symbol and place_holder_1 returns the names
    @staticmethod
    def match_regex(s):
        return "(" + s + R"()\s*([a-zA-Z0-9_]*).*)"

    #  Comment regex
    r_one_line_unstructured_comment         = re.compile('(.*?)(\/\/)(.*)\s*')
    r_one_line_structured_comment           = re.compile('(.*?)(<\*\*)(.*?)\*\*>\s*')
    r_multi_line_unstructured_comment_begin = re.compile('(.*?)(\/\*)(.*)\s*')
    r_multi_line_unstructured_comment_end   = re.compile('(.*?)(\*\/)(.*)\s*')
    r_multi_line_structured_comment_begin   = re.compile('(.*?)(\<\*\*)(.*)\s*')
    r_multi_line_structured_comment_end     = re.compile('(.*?)(\*\*\>)(.*)\s*')
    r_structured_comment                    = re.compile('@(.*?):([^@]*)')

    r_get_id                                = re.compile('(id) *= *([a-fA-Fx0-9]*)(.*?)')
    r_get_size                              = re.compile('(size) *= *(\d*)')
    r_type_check                            = re.compile('(?:([a-zA-Z0-9_]*)\.)?([a-zA-Z0-9_]*)(\[\])?\s+([a-zA-Z0-9_]*)\s*')

    #  Element regex
    r_package           = re.compile('(package)\s*(.*)')
    r_interface         = re.compile(match_regex('interface'))
    r_type_collection   = re.compile(match_regex('typeCollection'))
    r_version           = re.compile('(version)\s*')
    r_version_value     = re.compile('(major)\s*(\d*)\s*minor\s*(\d*)')
    r_array             = re.compile('.*(array)\s*([a-zA-Z0-9]*)\s*of\s*([a-zA-Z0-9]*)')
    r_enumeration       = re.compile(match_regex('enumeration'))
    r_enum_value        = re.compile('([a-zA-Z0-9_]*),?\s*(?:=\s*([a-fA-Fx0-9]*))?,?')
    r_struct            = re.compile(match_regex('struct'))
    r_method            = re.compile(match_regex('method'))
    r_method_in         = re.compile('(in)\s*')
    r_method_out        = re.compile('(out)\s*')
    r_method_error      = re.compile('(error)\s*')
    r_union             = re.compile(match_regex('union'))
    r_map               = re.compile(match_regex('map'))
    r_typedef           = re.compile('.*(typedef)\s*([a-zA-Z0-9]*)\s*is\s*([a-zA-Z0-9]*)')
    r_extends           = re.compile('.*(extends)\s*([a-zA-Z0-9_]*).*')
#endregion

#region Main
    def __init__(self):
        # Initialized the parser
        # General
        self.line                   = 0
        self.file_path              = ""
        self.output_path            = ""
        self.context                = FIDLParser.Context()

        # Comment Related
        self.isMultiComment         = False
        self.current_comment        = ""
        self.current_tag            = FIDLParser.Tag()

        # Additional
        self.enum_counter           = 0

        # Scope Related
        self.scope_stack            = [FIDLParser.Scope.Root]
        self.scope_level            = 0
        self.current_scope          = FIDLParser.Scope.Root
        self.function_map: Dict[FIDLParser.Scope, Callable[[str], None]] \
                                    = {FIDLParser.Scope.Root: self.s_root,
                                       FIDLParser.Scope.TypeCollection: self.s_type_collection,
                                       FIDLParser.Scope.Interface: self.s_interface,
                                       FIDLParser.Scope.Version: self.s_version,
                                       FIDLParser.Scope.Enumeration: self.s_enumeration,
                                       FIDLParser.Scope.Struct: self.s_struct,
                                       FIDLParser.Scope.Method: self.s_method,
                                       FIDLParser.Scope.MethodIn: self.s_method_in,
                                       FIDLParser.Scope.MethodOut: self.s_method_out,
                                       FIDLParser.Scope.MethodError: self.s_method_error,
                                       FIDLParser.Scope.Union: self.s_union,
                                       FIDLParser.Scope.Map: self.s_map,
                                       FIDLParser.Scope.Broadcast: self.s_broadcast,
                                       FIDLParser.Scope.BroadcastOut: self.s_broadcast_out,
                                       FIDLParser.Scope.PSM: self.s_psm,
                                       FIDLParser.Scope.PSMState: self.s_psm_state}
        self.str_add_null:bool = False

    def clear(self):
        self.line = 0
        self.file_path = ""
        self.output_path = ""
        self.context = FIDLParser.Context()
        self.isMultiComment = False
        self.current_comment = ""
        self.current_tag = FIDLParser.Tag()
        self.scope_stack = [FIDLParser.Scope.Root]
        self.scope_level = 0
        self.current_scope = FIDLParser.Scope.Root

    ## parse
    #  @description: parse the FIDL File
    def parse(self, file_path: str, output_path: str, debug: bool = False, str_add_null:bool = False) -> bool:
        self.file_path = file_path
        self.output_path = output_path
        is_success = True
        try:
            with open(self.file_path, 'r', encoding='utf-8') as f:
                for s in f:
                    self.line += 1
                    s = self.preprocess(s)

                    if s == "":
                        continue
                    self.local_parsing(s)
            self.export()
        except FIDLParser.ParseError:
            is_success = False
        except Exception as e:
            is_success = False
            self.log_e(e)

        if debug:
            self.parsed_result()

        self.str_add_null = str_add_null


        self.clear()
        return is_success

    def export(self):
        data = OrderedDict()
        data["package"] = self.context.package
        if len(self.context.interfaces) > 1:
            self.log_e("Multiple interfaces are not yet supported")
        interface = self.context.interfaces[0]
        data["interface"] = interface.name
        data["description"] = interface.description
        data["id"] = interface.id
        data["version"] = {"major":interface.major, "minor": interface.minor}

        enum_elements = []
        struct_elements = []
        for node in interface.nodes:
            el = OrderedDict()
            el["name"] = node.name
            el["description"] = node.description

            if node.type == FIDLParser.Type.Enumeration:
                enum_elements.append(el)
                values = []
                for v in node.sub_nodes:
                    vl = OrderedDict()
                    vl["name"] = v.name
                    vl["description"] = v.description
                    vl["id"] = v.id
                    values.append(vl)
                el["values"] = values

            elif node.type == FIDLParser.Type.Struct:
                el["size"] = node.id
                struct_elements.append(el)

                values = []
                for v in node.sub_nodes:
                    vl = OrderedDict()
                    vl["name"] = v.name
                    vl["description"] = v.description
                    type_string = v.type.base().name if v.type.base() != FIDLParser.Type.UserDefined \
                        else FIDLParser.Type.UserDefined.name + "|" + v.info
                    vl["type"] = type_string
                    vl["is_array"] = v.type.is_array()
                    vl["size"] = v.id
                    values.append(vl)
                el["values"] = values
            else:
                self.log_e("???")
        data["enumeration"] = enum_elements
        data["struct"] = struct_elements

        method_list = []
        for m in interface.methods:
            el = OrderedDict()
            el["name"] = m.name
            el["description"] = m.description
            el["id"] = m.id

            in_list = []
            for ie in m.input:
                vl = OrderedDict()
                vl["name"] = ie.name
                vl["description"] = ie.description
                type_string = ie.type.base().name if ie.type.base() != FIDLParser.Type.UserDefined \
                    else FIDLParser.Type.UserDefined.name + "|" + ie.info
                vl["type"] = type_string
                vl["is_array"] = ie.type.is_array()
                vl["size"] = ie.id
                in_list.append(vl)
            el["in"] = in_list

            out_list = []
            for oe in m.output:
                vl = OrderedDict()
                vl["name"] = oe.name
                vl["description"] = oe.description
                type_string = oe.type.base().name if oe.type.base() != FIDLParser.Type.UserDefined \
                    else FIDLParser.Type.UserDefined.name + "|" + oe.info
                vl["type"] = type_string
                vl["is_array"] = oe.type.is_array()
                vl["size"] = oe.id
                out_list.append(vl)
            el["out"] = out_list

            method_list.append(el)

        data["method"] = method_list

        path = self.output_path + "\\" + os.path.basename(self.file_path).split('.')[0] + ".json"
        if TYPE_CHECKING:
            from _typeshed import SupportsWrite
            make_file: SupportsWrite[str]
        with open(path,'w', encoding='utf-8') as make_file:
            json.dump(data, make_file, ensure_ascii=False, indent=2)


    ## parsed_result
    #  @description: print the parsed result
    def parsed_result(self):
        print("Package:", self.context.package)
        i = 0; j = 0
        for interface in self.context.interfaces:
            print("-Description:", interface.description)
            print("-Name:", interface.name)
            print("-ID:", interface.id)
            print("-Major:", interface.major)
            print("-Minor:", interface.minor)
            for node in interface.nodes:
                print("[Type%d]"%i); i+=1
                print("\tname:", node.name)
                print("\tdescription:", node.description)
                print("\tid:", node.id)
                print("\ttype:", node.type.name)
                for sub in node.sub_nodes:
                    print("\t\tname", sub.name,"/ id", sub.id, "/ type", sub.type.name, '/', sub.info)
                    print("\t\t\tdescription:", sub.description)
            for met in interface.methods:
                print("[Method%d]" % j); j += 1
                print("\tname:", met.name)
                print("\tdescription:", met.description)
                print("\tid:", met.id)
                print("\tMethod In")
                for inp in met.input:
                    print("\t\tname", inp.name,"/ id", inp.id, "/ type", inp.type.name, '/', inp.info)
                    print("\t\t\tdescription:", inp.description)
                print("\tMethod Out")
                for out in met.output:
                    print("\t\tname", out.name, "/ id", out.id, "/ type", out.type.name, '/', out.info)
                    print("\t\t\tdescription:", out.description)
#endregion

    ## parseComment
    #  @description: function to parse the comment
    #
    def parse_comment(self):
        matches = re.findall(self.r_structured_comment, self.current_comment)

        for m in matches:
            tag = str(m[0]).strip(); value = str(m[1]).strip()
            if tag == 'description':
                self.current_tag.set_description(value)
            elif tag == 'details':
                self.current_tag.set_details(value)
            else:
                self.log_w("Unsupported tag:", tag)
        self.current_comment = ""

    ## preprocess
    #  @description: function to remove and parse the comment
    #
    def preprocess(self, s: str):
        s.strip()
        # Remove Comment
        if not self.isMultiComment:
            [ret, s] = match(self.r_one_line_structured_comment, s)
            if ret:
                self.current_comment = ret.group(place_holder_2).strip()
                self.parse_comment()

            [ret, s] = match(self.r_one_line_unstructured_comment, s)

            [ret, s] = match(self.r_multi_line_unstructured_comment_begin, s)
            if ret:
                self.isMultiComment = True

            [ret, s] = match(self.r_multi_line_structured_comment_begin, s)
            if ret:
                self.current_comment = ret.group(place_holder_2).strip()
                self.isMultiComment = True
        else:
            [ret, s] = match(self.r_multi_line_structured_comment_end, s, place_holder_2)
            if ret:
                self.current_comment += ret.group(place_holder_0).strip()
                self.isMultiComment = False
                self.parse_comment()
                return s

            [ret, s] = match(self.r_multi_line_unstructured_comment_end, s, place_holder_2)
            if ret:
                self.isMultiComment = False

            self.current_comment += s
            s = ""
        return s.strip()

    ## local_parsing
    #  @description: parsing the preprocessed string
    #
    def local_parsing(self, s: str):
        if s == "":
            return
        pos_begin = s.find('{')
        pos_end   = s.find('}')
        pos = len(s)

        if pos_begin >= 0 > pos_end:
            pos = pos_begin
        elif pos_end >= 0 > pos_begin:
            pos = pos_end
        elif pos_begin >= 0 and pos_end >= 0:
            pos = min(pos_begin, pos_end)
        lstr = s[0:pos].strip()

        rstr = ""
        if len(self.scope_stack) == 0:
            self.log_e("Invalid bracket")

        func = self.function_map.get(self.scope_stack[-1])
        if not func:
            self.log_e("No implementation of scope function-", self.scope_stack[-1].name)
        if len(lstr) > 0:
            func(lstr)

        if pos == len(s):
            return
        if pos < len(s):
            rstr = s[pos+1:].strip()

        if pos == pos_begin:
            self.scope_level += 1
            self.scope_stack.append(self.current_scope)
        else:
            self.scope_level -= 1
            self.scope_stack.pop()
            if len(self.scope_stack) == 0:
                self.log_e("Invalid bracket")
            self.current_scope = self.scope_stack[-1]

        # Recursive call
        self.local_parsing(rstr)

    ## get_current_container
    #  @description: get current container
    def get_current_container(self) -> Context.Interface:
        containers = self.context.interfaces
        if not containers: self.log_e("No container")
        return containers[-1]

    ## add
    #  @description: add the node to the container
    def add(self, type_, name_: str):
        container = self.get_current_container()
        ret = container.find_node(name_)
        if ret:
            self.log_e("Parameter has the same name:", name_)

        [desc, deta] = self.current_tag.get()
        container.nodes.append(FIDLParser.Context.Node(name_, desc, self.get_id(deta), type_))

    ## add
    #  @description: add the node to the container
    def add_type(self, type_, name_: str):
        container = self.get_current_container()
        ret = container.find_node(name_)
        if ret:
            self.log_e("Parameter has the same name:", name_)

        [desc, deta] = self.current_tag.get()
        container.nodes.append(FIDLParser.Context.Node(name_, desc, self.get_size("", type_, ""), type_))

    ## add_extend
    #  @description: add the node from the extends to the container
    def add_extends(self, s: str):
        container = self.get_current_container()
        [ret, s] = match(self.r_extends, s, place_holder_none)
        if ret:
            name_ = ret.group(place_holder_1)
            node  = container.find_node(name_)
            if not node:
                self.log_e("Can't find extends:", name_)
            for t in node.sub_nodes:
                container.nodes[-1].sub_nodes.append(t)

    ## append
    #  @description: append the sub_nodes, method input or method output.
    def append(self, name_: str, value_: int, type_: 'FIDLParser.Type', method_: 'FIDLParser.MethodType' = MethodType.Null, info_: str = ""):
        container = self.get_current_container()
        [desc, deta] = self.current_tag.get()
        if method_ == FIDLParser.MethodType.Null:
            if value_ == -1:
                value_ = self.get_size(deta, type_, info_)
            container.nodes[-1].sub_nodes.append(FIDLParser.Context.Node(name_, desc, value_, type_, info_))
            if container.nodes[-1].type == FIDLParser.Type.Enumeration:
                container.nodes[-1].id = 1
            else:
                if container.nodes[-1].id == -1:
                    container.nodes[-1].id = 0
                container.nodes[-1].id += value_
        elif method_ == FIDLParser.MethodType.In:
            container.methods[-1].input.append(FIDLParser.Context.Node(name_, desc, self.get_size(deta, type_, info_), type_, info_))
        elif method_ == FIDLParser.MethodType.Out:
            container.methods[-1].output.append(FIDLParser.Context.Node(name_, desc, self.get_size(deta, type_, info_), type_, info_))

    ## type_check
    #  @description: check the type and append to the context
    def type_check(self, s: str, method_: 'FIDLParser.MethodType' = MethodType.Null):
        container = self.get_current_container()
        if method_ != FIDLParser.MethodType.Null and len(container.methods) == 0:
            self.log_e("No method")
        [ret, s] = match(self.r_type_check, s, place_holder_none)
        if ret:
            name_ = ret.group(place_holder_3)
            if not name_:
                self.log_w("Invalid method member name:",s)
            is_array = (ret.group(place_holder_2) == "[]")

            type_ = str(ret.group(place_holder_1))
            var = FIDLParser.Type.Null
            info_ = ""
            try:
                var = FIDLParser.Type[type_]
            except KeyError:
                tmp = container.find_node(type_)
                if tmp:
                    var = FIDLParser.Type.UserDefined
                    info_ = type_

            if var == FIDLParser.Type.Null:
                self.log_e("Unexpected type:", type_)

            self.append(name_, -1, var.to_array(is_array), method_, info_)


    ## add_element
    #  @description: specify the node and add it using 'add' and 'add_extend' method
    def add_element(self, s: str):
        container = self.get_current_container()

        # 1. Scope Version
        [ret, s] = match(self.r_version, s, place_holder_none)
        if ret:
            self.current_scope = FIDLParser.Scope.Version
            return

        # 2. Scope Array
        [ret, s] = match(self.r_array, s, place_holder_none)
        if ret:
            name_ = ret.group(place_holder_1)
            type_ = ret.group(place_holder_2)
            var = FIDLParser.Type.Null
            try:
                var = FIDLParser.Type[type_]
            except KeyError:
                var = container.find_type(type_)

            if var == FIDLParser.Type.Null:
                self.log_e("Unexpected type:", type_)

            self.add(var.to_array(), name_)
            return

        # 3. Enumeration
        [ret, s] = match(self.r_enumeration, s, place_holder_none)
        if ret:
            name_ = ret.group(place_holder_2)
            self.enum_counter = 0
            self.add(FIDLParser.Type.Enumeration, name_)
            self.current_scope = FIDLParser.Scope.Enumeration
            return

        # 4. Struct
        [ret, s] = match(self.r_struct, s, place_holder_none)
        if ret:
            name_ = ret.group(place_holder_2)
            self.add(FIDLParser.Type.Struct, name_)
            self.add_extends(s)
            self.current_scope = FIDLParser.Scope.Struct
            return

        # 5. Method
        [ret, s] = match(self.r_method, s, place_holder_none)
        if ret:
            name_ = ret.group(place_holder_2)
            container = self.get_current_container()
            [desc, deta] = self.current_tag.get()
            container.methods.append(FIDLParser.Context.Method(name_, desc, self.get_id(deta)))
            self.current_scope = FIDLParser.Scope.Method
            return

        # 6. Union
        [ret, s] = match(self.r_union, s, place_holder_none)
        if ret:
            name_ = ret.group(place_holder_2)
            self.add(FIDLParser.Type.Union, name_)
            self.add_extends(s)
            self.current_scope = FIDLParser.Scope.Union
            return

        # 7. Map
        [ret, s] = match(self.r_map, s, place_holder_none)
        if ret:
            name_ = ret.group(place_holder_2)
            self.add(FIDLParser.Type.Map, name_)
            self.current_scope = FIDLParser.Scope.Map
            return

        # 8. Typedef
        [ret, s] = match(self.r_typedef, s, place_holder_none)
        if ret:
            name_ = ret.group(place_holder_1)
            type_ = ret.group(place_holder_2)
            var = FIDLParser.Type.Null
            try:
                var = FIDLParser.Type[type_]
            except KeyError:
                var = container.find_type(type_)

            if var == FIDLParser.Type.Null:
                self.log_e("Unexpected type:", type_)

            self.add_type(var, name_)
            return

    ## get_id
    #  @description: get id from details
    def get_id(self, s: str) -> int:
        id_ = -1
        [ret, s] = match(self.r_get_id, s, place_holder_none)

        if ret:
            value = ret.group(place_holder_1).strip()
            try:
                if value.find('x') != -1 or value.find('X') != -1:
                    id_ = int(value, 16)
                else:
                    id_ = int(value)
            except ValueError:
                pass
        return id_

    ## get_size
    #  @description: get the size of the data
    def get_size(self, s: str, t: 'FIDLParser.Type', info: str) -> int:
        length = -1
        [ret, s] = match(self.r_get_size, s, place_holder_none)
        if ret:
            value = ret.group(place_holder_1)
            try:
                length = int(value)
            except ValueError:
                pass

        size = -1
        b = t.base()
        if b == FIDLParser.Type.UInt8 or b == FIDLParser.Type.Int8 or b == FIDLParser.Type.Boolean:
            size = 1
        elif b == FIDLParser.Type.UInt16 or b == FIDLParser.Type.Int16:
            size = 2
        elif b == FIDLParser.Type.UInt32 or b == FIDLParser.Type.Int32 or b == FIDLParser.Type.Integer:
            size = 4
        elif b == FIDLParser.Type.UInt64 or b == FIDLParser.Type.Int64:
            size = 8
        elif b == FIDLParser.Type.Float:
            size = 4
        elif b == FIDLParser.Type.Double:
            size = 8
        elif b == FIDLParser.Type.String:
            size = 1
        elif b == FIDLParser.Type.ByteBuffer:
            size = length
        elif b == FIDLParser.Type.Enumeration:
            size = 1
        elif b == FIDLParser.Type.UserDefined:
            container = self.get_current_container()
            node = container.find_node(info)
            size = node.id

        if size == -1:
            self.log_e("Can't infer the type size:", t.name)

        if t.is_array() or b == FIDLParser.Type.String:
            if length == -1:
                self.log_e("Can't infer the array type size:", t.name)
            size = size * length
            if b == FIDLParser.Type.String and self.str_add_null:
                size = size + 1
        return size

#region Scope functions
    def scope_check(self, level: int):
        if self.scope_level != level:
            self.log_e("Scope error")

    def s_root(self, s: str):
        self.scope_check(0)
        [ret, s] = match(self.r_package, s, place_holder_none)
        if ret:
            self.context.package = ret.group(place_holder_1)

        [ret, s] = match(self.r_interface, s, place_holder_none)
        if ret:
            name = ret.group(place_holder_2)
            [desc, deta] = self.current_tag.get()
            self.context.interfaces.append(FIDLParser.Context.Interface(name, desc, self.get_id(deta)))
            self.current_scope = FIDLParser.Scope.Interface

        [ret, s] = match(self.r_type_collection, s, place_holder_none)
        if ret:
            name = ret.group(place_holder_2)
            [desc, deta] = self.current_tag.get()
            self.context.interfaces.append(FIDLParser.Context.Interface(name, desc, self.get_id(deta)))
            self.current_scope = FIDLParser.Scope.TypeCollection

    def s_type_collection(self, s: str):
        self.scope_check(1)
        self.add_element(s)

    def s_interface(self, s: str):
        self.scope_check(1)
        self.add_element(s)

    def s_version(self, s: str):
        self.scope_check(2)
        [ret, s] = match(self.r_version_value, s, place_holder_none)
        if ret:
            container = self.get_current_container()
            container.major = int(ret.group(place_holder_1))
            container.minor = int(ret.group(place_holder_2))

    def s_enumeration(self, s: str):
        self.scope_check(2)
        [ret, s] = match(self.r_enum_value, s, place_holder_none)
        id_ = 0
        if ret:
            name = ret.group(place_holder_0)
            value = ret.group(place_holder_1)
            if not name: return
            if not value:
                id_ = self.enum_counter
                self.enum_counter += 1
            else:
                try:
                    if value.find('x') != -1 or value.find('X') != -1:
                        id_ = int(value, 16)
                    else:
                        id_ = int(value)
                    self.enum_counter = id_ + 1
                except ValueError:
                    id_ = self.enum_counter
                    self.enum_counter += 1
            self.append(name, id_, FIDLParser.Type.EnumElement)

    def s_struct(self, s: str):
        self.scope_check(2)
        self.type_check(s)

    def s_method(self, s: str):
        self.scope_check(2)
        [ret, s] = match(self.r_method_in, s, place_holder_none)
        if ret:
            self.current_scope = FIDLParser.Scope.MethodIn
            return
        [ret, s] = match(self.r_method_out, s, place_holder_none)
        if ret:
            self.current_scope = FIDLParser.Scope.MethodOut
            return
        [ret, s] = match(self.r_method_error, s, place_holder_none)
        if ret:
            self.current_scope = FIDLParser.Scope.MethodError
            return

    def s_method_in(self, s: str):
        self.scope_check(3)
        self.type_check(s, FIDLParser.MethodType.In)


    def s_method_out(self, s: str):
        self.scope_check(3)
        self.type_check(s, FIDLParser.MethodType.Out)

    def s_method_error(self, s: str):
        self.scope_check(3)
        self.log_e("No implementation of 'method_error'")

    def s_union(self, s: str):
        self.scope_check(2)
        self.log_e("No implementation of 'union'")

    def s_map(self, s: str):
        self.scope_check(2)
        self.log_e("No implementation of 'map'")

    def s_broadcast(self, s: str):
        self.scope_check(2)
        self.log_e("No implementation of 'broadcast'")

    def s_broadcast_out(self, s: str):
        self.scope_check(3)
        self.log_e("No implementation of 'broadcast_out'")

    def s_psm(self, s: str):
        self.scope_check(2)
        self.log_e("No implementation of 'psm'")

    def s_psm_state(self, s: str):
        self.scope_check(3)
        self.log_e("No implementation of 'psm_state'")
#endregion

#region Logging
    ## Logging the value
    def log_e(self, *args):
        print("\33[91m[ERROR] line", self.line,":", ' '.join(map(str,args)))
        raise FIDLParser.ParseError("Error")

    def log_i(self, *args):
        print("\033[0m[INFO] line", self.line,":", ' '.join(map(str,args)))

    def log_w(self, *args):
        print("\033[93m[WARN] line", self.line, ":", ' '.join(map(str,args)))

    banner = """\033[36m
    ______________  __          ____  ___    ____  _____ __________        
   / ____/  _/ __ \/ /         / __ \/   |  / __ \/ ___// ____/ __ \       
  / /_   / // / / / /         / /_/ / /| | / /_/ /\__ \/ __/ / /_/ /       
 / __/ _/ // /_/ / /___      / ____/ ___ |/ _, _/___/ / /___/ _, _/        
/_/   /___/_____/_____/     /_/   /_/  |_/_/ |_|/____/_____/_/ |_|         
\033[0m"""
#endregion

#region Main
def main():
    print(FIDLParser.banner)
    short_opts = "hf:o:d:s"
    long_opts  = ["help","file=","out=","output=","debug", "str"]
    try:
        opts, args = getopt.getopt(sys.argv[1:], short_opts, long_opts)
    except getopt.GetoptError as err:
        print(err)
        print("Use --help for usage information.")
        sys.exit(2)

    files = []
    output_dir = ""
    debug = False
    str_add_null = False
    for opt, arg in opts:
        if opt in ("-h", "--help"):
            print("Usage: FIDLParser.py [-f (FILE|DIR) | --file (FILE|DIR) | -o (DIR) | --out (DIR)]")
            print("Options:")
            print("  -h, --help   Show this help message and exit.")
            print("  -f, --file   Specify the input file or directory.")
            print("  -o, --out    Specify the output directory.")
            print("  -s, --str    Add null value for string.")
            sys.exit()
        elif opt in ("-f", "--file"):
            name = arg
            if os.path.isfile(name):
                if name.endswith('.fidl'):
                    files.append(name)
                else:
                    print("The extension of the file must be '*.fidl'")
            elif os.path.isdir(name):
                for path, sub_dirs, els in os.walk(name):
                    for n in els:
                        if n.endswith('.fidl'):
                            files.append(os.path.join(path,n))
            else:
                print("No such file or directory exists.", name)
        elif opt in ("-o", "--out", "--output"):
            name = arg
            if os.path.isdir(name):
                output_dir = name
            else:
                print("-o and --out argument must be directory.")
        elif opt in ("-d", "--debug"):
            debug = True
        elif opt in ("-s", "--str"):
            str_add_null = True

    # Parsing the file
    parser = FIDLParser()
    parse_count = 0
    success_count = 0
    fail_list = []
    for f in files:
        print("[%d]Parsing the file:" % parse_count, f)
        if output_dir == "":
            ret = parser.parse(str(f), os.path.dirname(f), debug, str_add_null)
        else:
            ret = parser.parse(str(f), output_dir, debug, str_add_null)
        print("\033[0m[%d]Finished" % parse_count)
        if ret:
            success_count += 1
        else:
            fail_list.append((parse_count, f))
        parse_count += 1
    print("=> Total:", len(files)," Success:", success_count, " Fail:", len(files) - success_count)
    if len(fail_list) > 0:
        print("> Fail List <")
        for fail in fail_list:
            print("[%d]\tPath = %s" %(fail[0], fail[1]))
if __name__ == "__main__":
    main()
#endregion


