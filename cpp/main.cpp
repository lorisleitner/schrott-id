#define CATCH_CONFIG_MAIN

#include "lib/catch2.hpp"

#include "schrott_id.hpp"

auto test_permutation = "HwEMFAcAMAYEPxc4Dy4RAxAkEgstJggbGSMiKB0yHgk7OSsNMxoYKRMWNg49LzEFFTQKPDUhHAIsICclOio+Nw==";

TEST_CASE("Encode and decode first 10000")
{
    s_id::schrott_id schrott_id(s_id::alphabets::base64, test_permutation, 3);

    for (std::uint64_t i = 0; i < 10000; ++i)
    {
        auto encoded = schrott_id.encode(i);
        auto decoded = schrott_id.decode(encoded);

        REQUIRE(decoded == i);
    }
}

TEST_CASE("Test encode decode control")
{
    // control.txt contains the encoded values from 0 to 9999

    std::ifstream control_file("../../test/control.txt");
    REQUIRE_FALSE(control_file.fail());

    std::string line;
    std::vector<std::string> control_lines;
    control_lines.reserve(10000);

    while (std::getline(control_file, line))
    {
        if (!line.empty()
            && line.find('#') != 0)
        {
            control_lines.emplace_back(std::move(line));
        }
    }

    s_id::schrott_id schrott_id(s_id::alphabets::base64, test_permutation, 3);

    std::vector<std::string> schrott_ids;
    schrott_ids.reserve(10000);

    for (auto i = 0; i < 10000; ++i)
    {
        schrott_ids.emplace_back(schrott_id.encode(i));
    }

    REQUIRE(std::equal(control_lines.begin(), control_lines.end(), schrott_ids.begin()));
}

TEST_CASE("Generate permutation")
{
    // This test depends on randomness, loop 1000 times to make sure we cover as many cases as possible

    for (auto i = 0; i < 1000; ++i)
    {
        auto permutationString = s_id::schrott_id::generate_permutation(s_id::alphabets::base64);

        auto permutation = s_id::base64::decode(permutationString);

        REQUIRE(permutation.size() == strlen(s_id::alphabets::base64));
        REQUIRE(s_id::util::is_unique(permutation.begin(), permutation.end()));

        int min_element = *std::min_element(permutation.begin(), permutation.end());
        int max_element = *std::max_element(permutation.begin(), permutation.end());

        REQUIRE(min_element == 0);
        REQUIRE(max_element == strlen(s_id::alphabets::base64) - 1);
    }
}