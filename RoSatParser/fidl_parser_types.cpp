#include "pch.h"
#include "fidl_parser_types.h"

#include <algorithm>
#ifdef _DEBUG
#include <iostream>
#endif
using namespace RoSatParser;

std::tuple<std::string, std::string> RoSatParser::FIDL::FIDLTag::getValue() {
	if (!valid) { return std::make_tuple("",""); }
	std::string desc;
	std::string deta;
	std::swap(desc, description);
	std::swap(deta, details);
	valid = false;
	std::string::iterator new_end = std::unique(desc.begin(), desc.end(), [](char lhs, char rhs) { return (lhs == rhs) && (lhs == ' '); });
	desc.erase(new_end, desc.end());
	desc.erase(std::remove(desc.begin(), desc.end(), '\t'), desc.end());
	return std::make_tuple(desc, deta);
}

void RoSatParser::FIDL::FIDLTag::setDescription(const std::string& str) {
	this->description = str;
	valid = true;
}

void RoSatParser::FIDL::FIDLTag::setDetails(const std::string& str) {
	this->details = str;
	valid = true;
}

std::string RoSatParser::FIDL::FIDLNode::getName() const {
	return this->name;
}

FIDL::FIDLType RoSatParser::FIDL::FIDLNode::getType() const {
	return this->type;
}

std::string RoSatParser::FIDL::FIDLNode::getDetail() const
{
	return this->detail;
}


FIDL::FIDLType RoSatParser::FIDL::FIDLContainer::findName(const std::string& name) const {
	//std::cout << "!" << name << std::endl;
	const auto& it = std::find_if(nodes.begin(), nodes.end(), [&name](const auto& p) { return p.get()->getName() == name; });
	if (it != nodes.end()) {
		return it->get()->getType();
	}
	return FIDLType::None;
}

FIDL::FIDLNode* RoSatParser::FIDL::FIDLContainer::findNode(const std::string& name) const {
	//std::cout << "!" << name << std::endl;
	const auto& it = std::find_if(nodes.begin(), nodes.end(), [&name](const auto& p) { return p.get()->getName() == name; });
	if (it != nodes.end()) {
		return it->get();
	}
	return nullptr;
}